using Microsoft.Extensions.Logging;
using TwitchUtils.Checkers;
using TwitchUtils.Checkers.Helix;
using TwitchUtils.Checkers.Pubsub;

namespace TwitchUtils;

/// <summary>
/// Следит за появлением стримов.
/// </summary>
public class TwitchStatuser
{
    private readonly ILogger? _logger;

    private bool _lastOnline = false;
    private DateTime? _lastOnlineUpdateDate = null;

    /// <summary>
    /// Если ивент пришёл от сомнительного источника, не принимать его во внимание, если последнее обновление было ранее этого количества времени.
    /// Хеликс имеет дурную привычку опаздывать с обновлением секунд на 5. Что даёт двойные срабатывания online ивента.
    /// </summary>
    private readonly TimeSpan _notTrustworthyUpdateDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Пришло обновление онлайн, и канал был оффлаин.
    /// </summary>
    public event Action<TwitchCheckInfo>? ChannelOnline;

    /// <summary>
    /// Пришло обновление оффлаин, и канал был онлайн.
    /// </summary>
    public event Action<TwitchCheckInfo>? ChannelOffline;

    private readonly object _locker = new();

    public TwitchStatuser(IEnumerable<ITwitchChecker> checkers,
        ILoggerFactory? loggerFactory = null)
    {
        this._logger = loggerFactory?.CreateLogger<TwitchStatuser>();

        foreach (var checker in checkers)
        {
            checker.ChannelChecked += ChannelChecked;
        }
    }

    private void ChannelChecked(object? sender, TwitchCheckInfo info)
    {
        try
        {
            // Можно было бы сравнить info.online и lastOnline
            // Но это было бы вне лока, а мне не хоетсй.
            bool statusChanged = false;

            lock (_locker)
            {
                if (info.Online)
                {
                    if (!_lastOnline)
                    {
                        _logger?.LogInformation("Стрим поднялся. {name}", sender?.GetType().Name);

                        _lastOnline = true;
                        statusChanged = true;
                    }
                }
                else
                {
                    if (_lastOnline)
                    {
                        _logger?.LogInformation("Стрим опустился. {name}", sender?.GetType().Name);

                        _lastOnline = false;
                        statusChanged = true;
                    }
                }
            }

            if (sender is not ITwitchChecker checker)
            {
                _logger?.LogCritical("Че это у вас тут происходит, чекер не чекер {name}", sender?.GetType().Name);
                return;
            }

            if (statusChanged && !checker.TrustWorthy)
            {
                TimeSpan? timePassedSinceLastUpdate = DateTime.UtcNow - _lastOnlineUpdateDate;

                statusChanged = timePassedSinceLastUpdate == null ||
                                timePassedSinceLastUpdate >= _notTrustworthyUpdateDelay;
            }

            if (statusChanged)
            {
                if (info.Online)
                    ChannelOnline?.Invoke(info);
                else
                    ChannelOffline?.Invoke(info);

                _lastOnlineUpdateDate = DateTime.UtcNow;
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Ошибка при обработке информации. {name}", sender?.GetType().Name);
        }
    }

    /// <summary>
    /// Создаёт и запускает всё.
    /// </summary>
    /// <returns></returns>
    public static async Task<TwitchStatuser> CreateAsync(TwitchStatuserConfig config,
        Action<TwitchStatuser> prelaunchDelegate, ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        List<ITwitchChecker> checkers = new();

        PubsubChecker? pubsub = null;
        HelixChecker? helix = null;

        if (config.UsePubsub)
        {
            pubsub = new(config, loggerFactory, cancellationToken);

            checkers.Add(pubsub);
        }

        if (config.Helix != null)
        {
            helix = new(config, loggerFactory, cancellationToken);

            checkers.Add(helix);
        }

        TwitchStatuser statuser = new(checkers, loggerFactory);

        prelaunchDelegate.Invoke(statuser);

        helix?.Start();
        if (pubsub != null)
        {
            await pubsub.StartAsync();
        }

        return statuser;
    }
}
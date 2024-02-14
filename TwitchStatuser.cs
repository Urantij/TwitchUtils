using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    readonly ILogger? logger;
    private readonly TwitchStatuserConfig config;

    bool lastOnline = false;

    /// <summary>
    /// Пришло обновление онлайн, и канал был оффлаин.
    /// </summary>
    public event Action<TwitchCheckInfo>? ChannelOnline;
    /// <summary>
    /// Пришло обновление оффлаин, и канал был онлайн.
    /// </summary>
    public event Action<TwitchCheckInfo>? ChannelOffline;
    /// <summary>
    /// Пришло обновление. <see cref="HelixCheck"/> приходит, даже если ничего не изменилось.
    /// </summary>
    public event Action<TwitchCheckInfo>? ChannelUpdate;

    readonly private object locker = new();

    public TwitchStatuser(TwitchStatuserConfig config, IEnumerable<ITwitchChecker> checkers, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
    {
        this.config = config;
        this.logger = loggerFactory?.CreateLogger<TwitchStatuser>();

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

            lock (locker)
            {
                if (info.online)
                {
                    if (!lastOnline)
                    {
                        logger?.LogInformation("Стрим поднялся. {name}", sender?.GetType().Name);

                        lastOnline = true;
                        statusChanged = true;
                    }
                }
                else
                {
                    if (lastOnline)
                    {
                        logger?.LogInformation("Стрим опустился. {name}", sender?.GetType().Name);

                        lastOnline = false;
                        statusChanged = true;
                    }
                }
            }

            if (statusChanged)
                if (info.online)
                    ChannelOnline?.Invoke(info);
                else
                    ChannelOffline?.Invoke(info);

            ChannelUpdate?.Invoke(info);
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Ошибка при обработке информации. {name}", sender?.GetType().Name);
        }
    }

    /// <summary>
    /// Создаёт и запускает всё.
    /// </summary>
    /// <returns></returns>
    public static async Task<TwitchStatuser> CreateAsync(TwitchStatuserConfig config, Action<TwitchStatuser> prelaunchDelegate, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
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

        TwitchStatuser statuser = new(config, checkers, loggerFactory, cancellationToken);

        prelaunchDelegate.Invoke(statuser);

        helix?.Start();
        if (pubsub != null)
        {
            await pubsub.StartAsync();
        }

        return statuser;
    }
}

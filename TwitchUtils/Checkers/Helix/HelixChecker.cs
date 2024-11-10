using Microsoft.Extensions.Logging;
using TwitchLib.Api;

namespace TwitchUtils.Checkers.Helix;

public class HelixChecker : ITwitchChecker
{
    private readonly ILogger<HelixChecker>? _logger;
    private readonly TwitchStatuserConfig _config;
    private readonly HelixConfig _helixConfig;

    private readonly TwitchAPI _api;

    private readonly CancellationToken _cancellationToken;

    public bool TrustWorthy => false;

    public event EventHandler<TwitchCheckInfo>? ChannelChecked;

    public HelixChecker(TwitchStatuserConfig config, ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        this._logger = loggerFactory?.CreateLogger<HelixChecker>();

        if (config.Helix == null)
        {
            throw new NullReferenceException(nameof(HelixConfig));
        }

        this._config = config;
        _helixConfig = this._config.Helix;
        this._cancellationToken = cancellationToken;

        _api = new TwitchAPI();
        _api.Settings.ClientId = _helixConfig.ClientId;
        _api.Settings.Secret = _helixConfig.Secret;
    }

    public void Start()
    {
        _logger?.LogInformation("Начинаем. Частота обновлений {time}", _helixConfig.HelixCheckDelay);

        Task.Run(CheckLoopAsync, _cancellationToken);
    }

    private async Task CheckLoopAsync()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            TwitchCheckInfo? checkInfo = await CheckChannelAsync();

            // Если ошибка, стоит подождать чуть больше обычного.
            if (checkInfo == null)
            {
                try
                {
                    await Task.Delay(_helixConfig.HelixCheckDelay.Multiply(1.5), _cancellationToken);
                }
                catch
                {
                    return;
                }

                continue;
            }

            try
            {
                ChannelChecked?.Invoke(this, checkInfo);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"{nameof(CheckLoopAsync)}");
            }

            try
            {
                await Task.Delay(_helixConfig.HelixCheckDelay, _cancellationToken);
            }
            catch
            {
                return;
            }
        }

        _logger?.LogInformation("Закончили.");
    }

    /// <returns>null, если ошибка внеплановая</returns>
    private async Task<TwitchCheckInfo?> CheckChannelAsync()
    {
        TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream;

        try
        {
            var response =
                await _api.Helix.Streams.GetStreamsAsync(userIds: [_config.ChannelId], first: 1);

            if (response.Streams.Length == 0)
            {
                return new TwitchCheckInfo(false, DateTime.UtcNow);
            }

            stream = response.Streams[0];

            if (!stream.Type.Equals("live", StringComparison.OrdinalIgnoreCase))
                return new TwitchCheckInfo(false, DateTime.UtcNow);
        }
        catch (TwitchLib.Api.Core.Exceptions.BadScopeException)
        {
            _logger?.LogWarning($"CheckChannel exception опять BadScopeException");

            return null;
        }
        catch (TwitchLib.Api.Core.Exceptions.InternalServerErrorException)
        {
            _logger?.LogWarning($"CheckChannel exception опять InternalServerErrorException");

            return null;
        }
        catch (HttpRequestException e)
        {
            _logger?.LogWarning("CheckChannel HttpRequestException: \"{Message}\"", e.Message);

            return null;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "CheckChannel");

            return null;
        }

        return new HelixCheck(true, DateTime.UtcNow,
            new TwitchChannelInfo(stream.Title, stream.GameName, stream.GameId, stream.ViewerCount));
    }
}
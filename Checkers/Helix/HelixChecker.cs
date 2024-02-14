using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;

namespace TwitchUtils.Checkers.Helix;

public class HelixChecker : ITwitchChecker
{
    private readonly ILogger<HelixChecker>? logger;
    private readonly TwitchStatuserConfig config;
    private readonly HelixConfig helixConfig;

    private readonly TwitchAPI api;

    private readonly CancellationToken cancellationToken;

    public event EventHandler<TwitchCheckInfo>? ChannelChecked;

    public HelixChecker(TwitchStatuserConfig config, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
    {
        this.logger = loggerFactory?.CreateLogger<HelixChecker>();

        if (config.Helix == null)
        {
            throw new NullReferenceException(nameof(HelixConfig));
        }

        this.config = config;
        helixConfig = this.config.Helix;
        this.cancellationToken = cancellationToken;

        api = new TwitchAPI();
        api.Settings.ClientId = helixConfig.ClientId;
        api.Settings.Secret = helixConfig.Secret;
    }

    public void Start()
    {
        logger?.LogInformation("Начинаем. Частота обновлений {time}", helixConfig.HelixCheckDelay);

        Task.Run(CheckLoopAsync);
    }

    async Task CheckLoopAsync()
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TwitchCheckInfo? checkInfo = await CheckChannelAsync();

            // Если ошибка, стоит подождать чуть больше обычного.
            if (checkInfo == null)
            {
                try
                {
                    await Task.Delay(helixConfig.HelixCheckDelay.Multiply(1.5), cancellationToken);
                }
                catch { return; }
                continue;
            }

            try
            {
                ChannelChecked?.Invoke(this, checkInfo);
            }
            catch (Exception e)
            {
                logger?.LogError(e, $"{nameof(CheckLoopAsync)}");
            }

            try
            {
                await Task.Delay(helixConfig.HelixCheckDelay, cancellationToken);
            }
            catch { return; }
        }

        logger?.LogInformation("Закончили.");
    }

    /// <returns>null, если ошибка внеплановая</returns>
    private async Task<TwitchCheckInfo?> CheckChannelAsync()
    {
        TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream;

        try
        {
            var response = await api.Helix.Streams.GetStreamsAsync(userIds: new List<string>() { config.ChannelId }, first: 1);

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
            logger?.LogWarning($"CheckChannel exception опять BadScopeException");

            return null;
        }
        catch (TwitchLib.Api.Core.Exceptions.InternalServerErrorException)
        {
            logger?.LogWarning($"CheckChannel exception опять InternalServerErrorException");

            return null;
        }
        catch (HttpRequestException e)
        {
            logger?.LogWarning("CheckChannel HttpRequestException: \"{Message}\"", e.Message);

            return null;
        }
        catch (Exception e)
        {
            logger?.LogError(e, "CheckChannel");

            return null;
        }

        return new HelixCheck(true, DateTime.UtcNow, new TwitchChannelInfo(stream.Title, stream.GameName, stream.GameId, stream.ViewerCount));
    }
}

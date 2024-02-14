using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwitchSimpleLib.Pubsub;
using TwitchSimpleLib.Pubsub.Payloads.Playback;

namespace TwitchUtils.Checkers.Pubsub;

public class PubsubChecker : ITwitchChecker, IDisposable
{
    private readonly ILogger<PubsubChecker>? logger;

    private readonly TwitchPubsubClient client;

    public event EventHandler<TwitchCheckInfo>? ChannelChecked;

    public PubsubChecker(TwitchStatuserConfig config, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
    {
        this.logger = loggerFactory?.CreateLogger<PubsubChecker>();

        client = new TwitchPubsubClient(new TwitchPubsubClientOpts()
        {
        }, loggerFactory, cancellationToken);
        client.Connected += ClientConnected;
        client.ConnectionClosed += ClientConnectionClosed;

        var topic = client.AddPlaybackTopic(config.ChannelId);
        topic.DataReceived = PlaybackReceived;
    }

    public async Task StartAsync()
    {
        logger?.LogInformation("Начинаем.");

        await client.ConnectAsync();
    }

    private void PlaybackReceived(PlaybackData data)
    {
        bool up;
        if (data.Type == "stream-up")
        {
            up = true;
        }
        else if (data.Type == "stream-down")
        {
            up = false;
        }
        else return;

        TwitchCheckInfo checkInfo = new(up, DateTime.UtcNow);

        try
        {
            ChannelChecked?.Invoke(this, checkInfo);
        }
        catch (Exception e)
        {
            logger?.LogError(e, $"{nameof(PlaybackReceived)}");
        }
    }

    private void ClientConnected()
    {
        logger?.LogInformation("Клиент присоединился.");
    }

    private void ClientConnectionClosed(Exception? exception)
    {
        if (logger != null)
        {
            logger.LogInformation("Клиент потерял соединение. {message}", exception?.Message);
            logger.LogDebug(exception, "Клиент потерял соединение.");
        }
    }

    public void Dispose()
    {
        client.Close();
    }
}

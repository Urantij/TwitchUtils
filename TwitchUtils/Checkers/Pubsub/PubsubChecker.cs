using Microsoft.Extensions.Logging;
using TwitchSimpleLib.Pubsub;
using TwitchSimpleLib.Pubsub.Payloads.Playback;

namespace TwitchUtils.Checkers.Pubsub;

public class PubsubChecker : ITwitchChecker, IDisposable
{
    private readonly ILogger<PubsubChecker>? _logger;

    private readonly TwitchPubsubClient _client;

    public bool TrustWorthy => true;

    public event EventHandler<TwitchCheckInfo>? ChannelChecked;

    public PubsubChecker(TwitchStatuserConfig config, ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        this._logger = loggerFactory?.CreateLogger<PubsubChecker>();

        _client = new TwitchPubsubClient(new TwitchPubsubClientOpts()
        {
        }, loggerFactory, cancellationToken);
        _client.Connected += ClientConnected;
        _client.ConnectionClosed += ClientConnectionClosed;

        var topic = _client.AddPlaybackTopic(config.ChannelId);
        topic.DataReceived = PlaybackReceived;
    }

    public async Task StartAsync()
    {
        _logger?.LogInformation("Начинаем.");

        await _client.ConnectAsync();
    }

    private void PlaybackReceived(PlaybackData data)
    {
        bool up;
        switch (data.Type)
        {
            case "stream-up":
                up = true;
                break;
            case "stream-down":
                up = false;
                break;
            default:
                return;
        }

        TwitchCheckInfo checkInfo = new(up, DateTime.UtcNow);

        try
        {
            ChannelChecked?.Invoke(this, checkInfo);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, $"{nameof(PlaybackReceived)}");
        }
    }

    private void ClientConnected()
    {
        _logger?.LogInformation("Клиент присоединился.");
    }

    private void ClientConnectionClosed(Exception? exception)
    {
        if (_logger == null) return;

        _logger.LogInformation("Клиент потерял соединение. {message}", exception?.Message);
        _logger.LogDebug(exception, "Клиент потерял соединение.");
    }

    public void Dispose()
    {
        _client.Close();
    }
}
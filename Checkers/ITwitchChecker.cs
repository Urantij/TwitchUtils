namespace TwitchUtils.Checkers;

public interface ITwitchChecker
{
    public bool TrustWorthy { get; }

    public event EventHandler<TwitchCheckInfo>? ChannelChecked;
}
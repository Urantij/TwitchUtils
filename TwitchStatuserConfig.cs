using TwitchUtils.Checkers.Helix;

namespace TwitchUtils;

public class TwitchStatuserConfig
{
    public required string ChannelId { get; set; }

    public HelixConfig? Helix { get; set; }

    public bool UsePubsub { get; set; } = true;
}
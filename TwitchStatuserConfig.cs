using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchUtils.Checkers.Helix;

namespace TwitchUtils;

public class TwitchStatuserConfig
{
    public required string ChannelId { get; set; }

    public HelixConfig? Helix { get; set; }

    public bool UsePubsub { get; set; } = true;
}

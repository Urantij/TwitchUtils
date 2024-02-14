using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchUtils.Checkers.Helix;

/// <summary>
/// Результат проверки канала от хеликса.
/// Поскольку пабсаб обычно быстрее хеликса, ловить этот чек лучше в <see cref="TwitchStatuser.ChannelUpdate"/>, а не в <see cref="TwitchStatuser.ChannelOnline"/>.
/// </summary>
public class HelixCheck : TwitchCheckInfo
{
    public readonly TwitchChannelInfo info;

    public HelixCheck(bool online, DateTime checkTime, TwitchChannelInfo info)
        : base(online, checkTime)
    {
        this.info = info;
    }
}

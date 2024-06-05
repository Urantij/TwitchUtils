namespace TwitchUtils.Checkers.Helix;

/// <summary>
/// Результат проверки канала от хеликса.
/// Поскольку пабсаб обычно быстрее хеликса, ловить этот чек лучше в <see cref="TwitchStatuser.ChannelUpdate"/>, а не в <see cref="TwitchStatuser.ChannelOnline"/>.
/// </summary>
public class HelixCheck(bool online, DateTime checkTime, TwitchChannelInfo info) : TwitchCheckInfo(online, checkTime)
{
    public readonly TwitchChannelInfo Info = info;
}
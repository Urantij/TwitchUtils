using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchUtils.Checkers.Helix;

/// <summary>
/// Доп инфа о стриме.
/// </summary>
public class TwitchChannelInfo
{
    public readonly string title;
    /// <summary>
    /// Нулл, если не удалось найти информацию.
    /// </summary>
    public readonly string gameName;
    public readonly string gameId;
    public readonly int viewers;

    public TwitchChannelInfo(string title, string gameName, string gameId, int viewers)
    {
        this.title = title;
        this.gameName = gameName;
        this.gameId = gameId;
        this.viewers = viewers;
    }
}

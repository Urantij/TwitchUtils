namespace TwitchUtils.Checkers.Helix;

/// <summary>
/// Доп инфа о стриме.
/// </summary>
public class TwitchChannelInfo(string title, string gameName, string gameId, int viewers)
{
    public readonly string Title = title;

    /// <summary>
    /// Нулл, если не удалось найти информацию.
    /// </summary>
    public readonly string GameName = gameName;

    public readonly string GameId = gameId;
    public readonly int Viewers = viewers;
}
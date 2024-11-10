namespace TwitchUtils.Checkers;

public class TwitchCheckInfo(bool online, DateTime checkTime)
{
    public readonly bool Online = online;

    /// <summary>
    /// UTC
    /// </summary>
    public readonly DateTime CheckTime = checkTime;
}
using TwitchUtils.Checkers.Helix;

namespace TwitchUtils;

public class TwitchStatuserConfig
{
    public required string ChannelId { get; set; }

    public HelixConfig? Helix { get; set; }

    public bool UsePubsub { get; set; } = true;

    /// <summary>
    /// Если ивент пришёл от сомнительного источника, не принимать его во внимание, если последнее обновление было ранее этого количества времени.
    /// Хеликс имеет дурную привычку опаздывать с обновлением секунд на 5. Что даёт двойные срабатывания online ивента.
    /// </summary>
    public TimeSpan NotTrustworthyUpdateDelay { get; set; } = TimeSpan.FromSeconds(15);
}
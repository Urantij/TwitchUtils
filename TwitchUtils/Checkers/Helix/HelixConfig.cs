using System.ComponentModel.DataAnnotations;

namespace TwitchUtils.Checkers.Helix;

public class HelixConfig
{
    [Required] public required string ClientId { get; set; }
    [Required] public required string Secret { get; set; }

    public TimeSpan HelixCheckDelay { get; set; } = TimeSpan.FromMinutes(1);
}
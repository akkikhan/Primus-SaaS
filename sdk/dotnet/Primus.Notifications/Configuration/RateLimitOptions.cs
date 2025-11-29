namespace PrimusSaaS.Notifications.Configuration;

public class RateLimitOptions
{
    /// <summary>
    /// Enable per-tenant/application rate limiting.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum notifications allowed per window.
    /// </summary>
    public int MaxPerWindow { get; set; } = 100;

    /// <summary>
    /// Sliding window size in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Optional hard cap per recipient (email/phone) to guard hot spots.
    /// </summary>
    public int? MaxPerRecipientPerWindow { get; set; }
}

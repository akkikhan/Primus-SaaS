namespace PrimusSaaS.Notifications.Configuration;

/// <summary>
/// Options controlling notification dispatch behavior.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// When true, <see cref="Core.NotificationService"/> throws <see cref="Core.NotificationFailedException"/>
    /// if no channel delivers the notification. Defaults to true to avoid silent failures.
    /// </summary>
    public bool ThrowOnFailure { get; set; } = true;

    /// <summary>
    /// When true, attempts to emit the notification to the Logger channel if all other channels fail.
    /// </summary>
    public bool FallbackToLogger { get; set; }

    /// <summary>
    /// When true and a queue is registered, failed notifications are enqueued once for retry.
    /// </summary>
    public bool QueueOnFailure { get; set; }

    /// <summary>
    /// Optional per-tenant and per-recipient rate limiting.
    /// </summary>
    public RateLimitOptions RateLimit { get; set; } = new();

    /// <summary>
    /// Optional webhooks for delivery outcomes.
    /// </summary>
    public WebhookOptions Webhooks { get; set; } = new();
}

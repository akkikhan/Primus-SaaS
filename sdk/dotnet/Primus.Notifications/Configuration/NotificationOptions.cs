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
}

using System.Collections.Generic;

namespace PrimusSaaS.Notifications.Abstractions;

public interface INotification
{
    /// <summary>
    /// The unique identifier for this notification type (e.g., "welcome-email", "order-shipped").
    /// Used for template resolution.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// The data model to be passed to the template.
    /// </summary>
    object Data { get; }

    /// <summary>
    /// The channels this notification should be sent to.
    /// </summary>
    IEnumerable<string> Channels { get; }
    
    /// <summary>
    /// The recipient's contact information.
    /// </summary>
    Recipient Recipient { get; }
}

public class Recipient
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DeviceToken { get; set; }
    public string? Name { get; set; }
}

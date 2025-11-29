using System;
using System.Collections.Generic;
using Primus.Notifications.Abstractions;

namespace Primus.Notifications.Core;

/// <summary>
/// Lightweight notification implementation for common scenarios (email, SMS).
/// </summary>
public sealed class BasicNotification : INotification
{
    public BasicNotification(string type, object data, Recipient recipient, IEnumerable<string> channels)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
        Channels = channels ?? Array.Empty<string>();
    }

    public string Type { get; }
    public object Data { get; }
    public IEnumerable<string> Channels { get; }
    public Recipient Recipient { get; }
}

/// <summary>
/// Direct email payload used when no template is available.
/// </summary>
public sealed record DirectEmailContent(string Subject, string Body);

/// <summary>
/// Direct SMS payload used when no template is available.
/// </summary>
public sealed record DirectSmsContent(string Message);

using System;
using System.Collections.Generic;
using PrimusSaaS.Notifications.Abstractions;

namespace PrimusSaaS.Notifications.Core;

public sealed record NotificationDeliveryRecord
{
    public Guid NotificationId { get; init; } = Guid.NewGuid();
    public string Type { get; init; } = string.Empty;
    public Recipient Recipient { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyCollection<ChannelDispatchResult> Channels { get; init; } = Array.Empty<ChannelDispatchResult>();
    public bool Success { get; init; }
    public string? FailureReason { get; init; }
}

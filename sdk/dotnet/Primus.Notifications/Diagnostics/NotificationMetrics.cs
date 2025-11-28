using System.Diagnostics.Metrics;

namespace Primus.Notifications.Diagnostics;

/// <summary>
/// Centralized metrics instruments for notifications.
/// </summary>
public static class NotificationMetrics
{
    private static readonly Meter Meter = new("Primus.Notifications", "1.0.0");

    public static readonly Counter<long> NotificationsSent = Meter.CreateCounter<long>(
        "primus_notifications_sent_total",
        description: "Total notifications sent successfully.");

    public static readonly Counter<long> NotificationsFailed = Meter.CreateCounter<long>(
        "primus_notifications_failed_total",
        description: "Total notifications that failed across all retries.");

    public static readonly Histogram<double> DispatchDurationMs = Meter.CreateHistogram<double>(
        "primus_notification_dispatch_duration_ms",
        description: "Dispatch duration per notification across all channels.",
        unit: "ms");

    public static readonly Counter<long> QueueLength = Meter.CreateCounter<long>(
        "primus_notification_queue_events",
        description: "Queue enqueue/dequeue events. Add 1 on enqueue, -1 on dequeue.");
}

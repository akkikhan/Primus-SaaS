using System.Threading;

namespace PrimusSaaS.Notifications.Diagnostics;

/// <summary>
/// Lightweight in-memory counters for quick diagnostics without external exporters.
/// </summary>
public static class NotificationRuntimeStats
{
    private static long _sent;
    private static long _failed;
    private static long _queued;
    private static long _dispatchDurationMs;

    public static void RecordQueued() => Interlocked.Increment(ref _queued);

    public static void RecordSent(double durationMs)
    {
        Interlocked.Increment(ref _sent);
        Interlocked.Add(ref _dispatchDurationMs, (long)durationMs);
    }

    public static void RecordFailed() => Interlocked.Increment(ref _failed);

    public static Snapshot GetSnapshot()
    {
        var sent = Interlocked.Read(ref _sent);
        var failed = Interlocked.Read(ref _failed);
        var queued = Interlocked.Read(ref _queued);
        var totalDuration = Interlocked.Read(ref _dispatchDurationMs);
        var avgDuration = sent > 0 ? (double)totalDuration / sent : 0;

        return new Snapshot(sent, failed, queued, avgDuration);
    }

    public record Snapshot(long Sent, long Failed, long Queued, double AvgDispatchDurationMs);
}

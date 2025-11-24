using System.Threading;

namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Lightweight counters for logging observability (drops, writes, failures).
/// </summary>
public class LoggingMetrics
{
    private long _droppedEntries;
    private long _writtenEntries;
    private long _writeFailures;

    public void IncrementDropped() => Interlocked.Increment(ref _droppedEntries);

    public void IncrementWritten() => Interlocked.Increment(ref _writtenEntries);

    public void IncrementFailure() => Interlocked.Increment(ref _writeFailures);

    public LoggingMetricsSnapshot Snapshot()
    {
        return new LoggingMetricsSnapshot(
            Interlocked.Read(ref _droppedEntries),
            Interlocked.Read(ref _writtenEntries),
            Interlocked.Read(ref _writeFailures));
    }
}

public record LoggingMetricsSnapshot(long DroppedEntries, long WrittenEntries, long WriteFailures);

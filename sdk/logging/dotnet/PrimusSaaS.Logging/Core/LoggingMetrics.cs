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
    private long _adapterForwardedEntries;

    public void IncrementDropped() => Interlocked.Increment(ref _droppedEntries);

    public void IncrementWritten() => Interlocked.Increment(ref _writtenEntries);

    public void IncrementFailure() => Interlocked.Increment(ref _writeFailures);

    /// <summary>
    /// Tracks entries that flowed through the Microsoft.Extensions.Logging compatibility shim.
    /// </summary>
    public void IncrementAdapterForwarded() => Interlocked.Increment(ref _adapterForwardedEntries);

    public LoggingMetricsSnapshot Snapshot()
    {
        return new LoggingMetricsSnapshot(
            Interlocked.Read(ref _droppedEntries),
            Interlocked.Read(ref _writtenEntries),
            Interlocked.Read(ref _writeFailures),
            Interlocked.Read(ref _adapterForwardedEntries));
    }
}

public record LoggingMetricsSnapshot(long DroppedEntries, long WrittenEntries, long WriteFailures, long AdapterForwardedEntries);

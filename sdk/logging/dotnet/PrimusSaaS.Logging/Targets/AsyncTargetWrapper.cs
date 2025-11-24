using System.Collections.Concurrent;
using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Targets;

/// <summary>
/// Wraps a target to provide asynchronous buffering
/// </summary>
public class AsyncTargetWrapper : ITarget, IDisposable
{
    private readonly ITarget _innerTarget;
    private readonly BlockingCollection<LogEntry> _buffer;
    private readonly Task _workerTask;
    private readonly CancellationTokenSource _cts;
    private readonly LoggingMetrics? _metrics;
    private bool _disposed;

    public AsyncTargetWrapper(ITarget innerTarget, int bufferSize = 1000, LoggingMetrics? metrics = null)
    {
        _innerTarget = innerTarget;
        _buffer = new BlockingCollection<LogEntry>(bufferSize);
        _cts = new CancellationTokenSource();
        _metrics = metrics;
        _workerTask = Task.Factory.StartNew(
            ProcessLogQueue,
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public void Write(LogEntry logEntry)
    {
        if (_disposed || _buffer.IsAddingCompleted) return;

        try
        {
            // Try to add to buffer, drop if full to avoid blocking application
            // Alternatively, we could block with a timeout
            if (!_buffer.TryAdd(logEntry))
            {
                // Buffer full - strategy: drop or fallback?
                // For high-perf logging, dropping is often preferred over blocking
                // But let's try a short wait
                if (!_buffer.TryAdd(logEntry, 10))
                {
                    _metrics?.IncrementDropped();
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Collection marked as complete
        }
    }

    private void ProcessLogQueue()
    {
        try
        {
            foreach (var entry in _buffer.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    _innerTarget.Write(entry);
                    _metrics?.IncrementWritten();
                }
                catch (Exception ex)
                {
                    _metrics?.IncrementFailure();
                    Console.Error.WriteLine($"Async target error: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            // Ensure remaining items are processed if not cancelled abruptly
            while (_buffer.TryTake(out var entry))
            {
                try { _innerTarget.Write(entry); } catch { }
            }
        }
    }

    public void Close()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _buffer.CompleteAdding();
        
        try
        {
            // Wait for worker to finish processing buffer
            _workerTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Ignore task cancellation exceptions
        }

        _cts.Cancel();
        _innerTarget.Close();
        
        _cts.Dispose();
        _buffer.Dispose();
    }
}

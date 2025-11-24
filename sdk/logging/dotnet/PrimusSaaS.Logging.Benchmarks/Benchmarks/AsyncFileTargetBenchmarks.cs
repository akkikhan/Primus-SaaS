using BenchmarkDotNet.Attributes;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;

namespace PrimusSaaS.Logging.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class AsyncFileTargetBenchmarks : IDisposable
{
    private string _tempFile = null!;
    private AsyncTargetWrapper _asyncTarget = null!;
    private LogEntry _entry = null!;

    [GlobalSetup]
    public void Setup()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"primus_bench_{Guid.NewGuid():N}.log");
        var fileTarget = new FileTarget(_tempFile, maxFileSize: 10_000_000, maxRetainedFiles: 1, compress: false);
        _asyncTarget = new AsyncTargetWrapper(fileTarget, bufferSize: 1024);
        _entry = LogEntry.Create(LogLevel.Info, "Benchmark message", new Dictionary<string, object?>
        {
            ["userId"] = "user-123",
            ["orderId"] = "order-789"
        });
    }

    [Benchmark]
    public void WriteAsyncBuffered()
    {
        _asyncTarget.Write(_entry);
    }

    public void Dispose()
    {
        _asyncTarget?.Close();
        try { if (File.Exists(_tempFile)) File.Delete(_tempFile); } catch { }
    }
}

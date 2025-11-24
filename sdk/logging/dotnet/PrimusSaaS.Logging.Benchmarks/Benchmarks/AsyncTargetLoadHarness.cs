using BenchmarkDotNet.Attributes;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;

namespace PrimusSaaS.Logging.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class AsyncTargetLoadHarness : IDisposable
{
    private AsyncTargetWrapper _asyncTarget = null!;
    private LogEntry _entry = null!;

    [Params(100, 1000)]
    public int BufferSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _asyncTarget = new AsyncTargetWrapper(new NullTarget(), bufferSize: BufferSize);
        _entry = LogEntry.Create(LogLevel.Info, "msg", new Dictionary<string, object?>
        {
            ["user"] = "abc",
            ["order"] = "123"
        });
    }

    [Benchmark]
    public void PushToBuffer()
    {
        _asyncTarget.Write(_entry);
    }

    public void Dispose()
    {
        _asyncTarget?.Close();
    }

    private class NullTarget : ITarget
    {
        public void Write(LogEntry logEntry) { }
        public void Close() { }
    }
}

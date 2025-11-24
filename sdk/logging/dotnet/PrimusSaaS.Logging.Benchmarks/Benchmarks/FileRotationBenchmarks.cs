using BenchmarkDotNet.Attributes;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;

namespace PrimusSaaS.Logging.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class FileRotationBenchmarks : IDisposable
{
    private string _tempFile = null!;
    private FileTarget _target = null!;
    private LogEntry _entry = null!;

    [GlobalSetup]
    public void Setup()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"primus_bench_rotate_{Guid.NewGuid():N}.log");
        // Small size to force rotation quickly
        _target = new FileTarget(_tempFile, maxFileSize: 2048, maxRetainedFiles: 2, compress: false);
        _entry = LogEntry.Create(LogLevel.Info, "rotation test", new Dictionary<string, object?>
        {
            ["payload"] = new string('x', 500)
        });
    }

    [Benchmark]
    public void WriteWithRotation()
    {
        _target.Write(_entry);
    }

    public void Dispose()
    {
        _target?.Close();
        TryDelete(_tempFile);
        TryDelete(_tempFile + ".1");
        TryDelete(_tempFile + ".2");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}

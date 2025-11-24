using BenchmarkDotNet.Attributes;
using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class FormatterBenchmarks
{
    private SafeLogFormatter _formatter = null!;
    private SafeObjectSerializer _serializer = null!;
    private Dictionary<string, object?> _context = null!;
    private LogEntry _entry = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new SerializationOptions
        {
            MaxContextBytes = 64 * 1024
        };
        _serializer = new SafeObjectSerializer(options);
        _formatter = new SafeLogFormatter(_serializer);
        _context = _serializer.SanitizeContext(new Dictionary<string, object?>
        {
            ["userId"] = "user-123",
            ["orderId"] = "order-789",
            ["amount"] = 99.95m,
            ["claims"] = Enumerable.Range(0, 10).Select(i => new { type = "t" + i, value = "v" + i }).ToList()
        });
        _entry = LogEntry.Create(LogLevel.Info, "Order processed", _context, _formatter);
    }

    [Benchmark]
    public string FormatLogEntry()
    {
        return _entry.ToJson();
    }
}

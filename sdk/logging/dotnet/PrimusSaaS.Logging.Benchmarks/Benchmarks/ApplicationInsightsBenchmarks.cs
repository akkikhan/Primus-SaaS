using BenchmarkDotNet.Attributes;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;

namespace PrimusSaaS.Logging.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ApplicationInsightsBenchmarks
{
    private ApplicationInsightsTarget _target = null!;
    private LogEntry _entry = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Using default telemetry configuration; this benchmark focuses on serialization overhead
        _target = new ApplicationInsightsTarget(connectionString: null);
        _entry = LogEntry.Create(LogLevel.Info, "AI benchmark", new Dictionary<string, object?>
        {
            ["userId"] = "user-123",
            ["orderId"] = "ord-789",
            ["amount"] = 123.45m
        });
    }

    [Benchmark]
    public void WriteTrace()
    {
        _target.Write(_entry);
    }
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;
using System.CommandLine;
using PrimusSaaS.Logging.Benchmarks.Benchmarks;

// Dual mode:
// - Default: run BenchmarkDotNet
// - --load:durationSeconds 60 --rate 5000 to run a simple load harness

if (args.Contains("--load"))
    {
        await LoadHarness.Run(args);
    }
    else
    {
    BenchmarkRunner.Run(new[]
    {
        typeof(LoggingBenchmarks),
        typeof(FormatterBenchmarks),
        typeof(AsyncFileTargetBenchmarks),
        typeof(AsyncChannelBenchmarks),
        typeof(AsyncTargetLoadHarness),
        typeof(ApplicationInsightsBenchmarks),
        typeof(FileRotationBenchmarks)
    });
}

public class LoggingBenchmarks
{
    private PrimusSaaS.Logging.Core.Logger _logger = null!;
    private Dictionary<string, object?> _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        _logger = new PrimusSaaS.Logging.Core.Logger(new LoggerOptions
        {
            ApplicationId = "BENCH",
            Environment = "dev",
            MinLevel = LogLevel.Info,
            Targets = new List<TargetConfig>(),
            CustomTargets = new List<ITarget> { new NullTarget() },
            Serialization = new SerializationOptions
            {
                MaxContextBytes = 64 * 1024
            }
        });

        _context = new Dictionary<string, object?>
        {
            ["userId"] = "user-123",
            ["orderId"] = "order-789",
            ["amount"] = 99.95m,
            ["claims"] = Enumerable.Range(0, 5).Select(i => new { type = "t" + i, value = "v" + i }).ToList()
        };
    }

    [Benchmark]
    public void Info_WithContext()
    {
        _logger.Info("Order processed", _context);
    }

    private class NullTarget : ITarget
    {
        public void Write(LogEntry logEntry) { /* discard */ }
        public void Close() { }
    }
}

internal static class LoadHarness
{
    public static async Task Run(string[] args)
    {
        var rateOption = new Option<int>("--rate", () => 5000, "Logs per second");
        var durationOption = new Option<int>("--durationSeconds", () => 60, "Duration in seconds");
        var maxDropsOption = new Option<int?>("--maxDrops", description: "Optional max allowed drops; exits non-zero if exceeded.");
        var maxFailuresOption = new Option<int?>("--maxFailures", description: "Optional max allowed write failures; exits non-zero if exceeded.");
        var root = new RootCommand { rateOption, durationOption };
        root.AddOption(maxDropsOption);
        root.AddOption(maxFailuresOption);

        var parsed = root.Parse(args);
        var rate = parsed.GetValueForOption(rateOption);
        var durationSeconds = parsed.GetValueForOption(durationOption);
        var maxDrops = parsed.GetValueForOption(maxDropsOption);
        var maxFailures = parsed.GetValueForOption(maxFailuresOption);

        var metrics = new PrimusSaaS.Logging.Core.LoggingMetrics();
        var logger = new PrimusSaaS.Logging.Core.Logger(new LoggerOptions
        {
            ApplicationId = "LOAD",
            Environment = "dev",
            MinLevel = LogLevel.Info,
            Targets = new List<TargetConfig>(),
            CustomTargets = new List<ITarget> { new NullTarget() },
            Metrics = metrics,
            Serialization = new SerializationOptions
            {
                MaxContextBytes = 32 * 1024
            }
        });

        var context = new Dictionary<string, object?>
        {
            ["userId"] = "user-123",
            ["session"] = Guid.NewGuid().ToString("N"),
            ["claims"] = Enumerable.Range(0, 3).Select(i => new { type = "t" + i, value = "v" + i }).ToList()
        };

        Console.WriteLine($"Running load: rate={rate}/s duration={durationSeconds}s");
        var interval = TimeSpan.FromSeconds(1.0 / rate);
        var stopAt = DateTime.UtcNow.AddSeconds(durationSeconds);
        var sent = 0;

        while (DateTime.UtcNow < stopAt)
        {
            logger.Info("Load message", context);
            sent++;
            await Task.Delay(interval);
        }

        var snapshot = metrics.Snapshot();
        Console.WriteLine("Load complete");
        Console.WriteLine($"Sent: {sent}, Written: {snapshot.WrittenEntries}, Drops: {snapshot.DroppedEntries}, Failures: {snapshot.WriteFailures}");

        if (maxDrops.HasValue && snapshot.DroppedEntries > maxDrops.Value)
        {
            Console.Error.WriteLine($"FAIL: Drops {snapshot.DroppedEntries} exceeded allowed {maxDrops.Value}");
            Environment.ExitCode = 1;
        }

        if (maxFailures.HasValue && snapshot.WriteFailures > maxFailures.Value)
        {
            Console.Error.WriteLine($"FAIL: Failures {snapshot.WriteFailures} exceeded allowed {maxFailures.Value}");
            Environment.ExitCode = 1;
        }
    }

    private class NullTarget : ITarget
    {
        public void Write(LogEntry logEntry) { }
        public void Close() { }
    }
}

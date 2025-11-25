using NLog;
using NLog.Config;
using NLog.Targets;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using PrimusLogger = PrimusSaaS.Logging.Core.Logger;
using NLogLogLevel = NLog.LogLevel;

namespace PrimusSaaS.Logging.Tests;

public class SerilogNLogBridgeTests
{
    [Fact]
    public void SerilogTarget_ForwardsEntriesToSink()
    {
        var sink = new InMemorySerilogSink();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var logger = new PrimusLogger(new LoggerOptions
        {
            ApplicationId = "TEST",
            Environment = "test",
            Targets = new List<TargetConfig> { new() { Type = "serilog" } }
        });

        logger.Info("hello serilog", new Dictionary<string, object?> { ["user"] = "abc" });

        var evt = Assert.Single(sink.Events);
        Assert.Equal(LogEventLevel.Information, evt.Level);
        Assert.Equal("hello serilog", evt.MessageTemplate.Text);
        Assert.Equal("abc", evt.Properties["user"].LiteralValue());
        Assert.Equal("TEST", evt.Properties["applicationId"].LiteralValue());
    }

    [Fact]
    public void NLogTarget_ForwardsEntriesToMemoryTarget()
    {
        var memory = new MemoryTarget("mem")
        {
            Layout = "${message}|${all-event-properties}"
        };
        var config = new LoggingConfiguration();
        config.AddRule(NLogLogLevel.Debug, NLogLogLevel.Fatal, memory);
        LogManager.Configuration = config;

        var logger = new PrimusLogger(new LoggerOptions
        {
            ApplicationId = "TEST",
            Environment = "test",
            Targets = new List<TargetConfig> { new() { Type = "nlog" } }
        });

        logger.Warn("warn nlog", new Dictionary<string, object?> { ["user"] = "abc" });

        var entry = Assert.Single(memory.Logs);
        Assert.Contains("warn nlog", entry);
        Assert.Contains("user=abc", entry.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InMemorySerilogSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new();

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}

internal static class SerilogPropertyExtensions
{
    public static object? LiteralValue(this LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue scalar => scalar.Value,
            _ => value.ToString()
        };
    }
}

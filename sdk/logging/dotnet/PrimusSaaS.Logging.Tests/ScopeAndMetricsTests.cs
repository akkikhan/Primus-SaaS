using System.Text.Json;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class ScopeAndMetricsTests : IDisposable
{
    private readonly string _tempDir;

    public ScopeAndMetricsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PrimusLoggingTests_Scope_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void BeginScope_ShouldFlowIntoLogContext()
    {
        var inMemory = new TestTarget();
        var options = new LoggerOptions
        {
            ApplicationId = "TEST",
            Environment = "dev",
            CustomTargets = new List<ITarget> { inMemory }
        };

        var logger = new Logger(options);

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["correlationId"] = "corr-123"
        }))
        {
            logger.Info("scoped message");
        }

        Assert.Single(inMemory.Logs);
        var entry = inMemory.Logs.Single();
        var context = entry.Context;
        Assert.Equal("corr-123", context["correlationId"]);
    }

    [Fact]
    public void AsyncWrapper_ShouldCountDroppedEntries()
    {
        var metrics = new LoggingMetrics();
        var slowTarget = new SlowTarget();
        var asyncWrapper = new AsyncTargetWrapper(slowTarget, bufferSize: 1, metrics: metrics);

        var entry = LogEntry.Create(LogLevel.Info, "msg");
        asyncWrapper.Write(entry);
        asyncWrapper.Write(entry);
        asyncWrapper.Write(entry);

        // Wait for processing
        Thread.Sleep(200);
        asyncWrapper.Close();

        var snapshot = metrics.Snapshot();
        Assert.True(snapshot.DroppedEntries >= 1, "Expected at least one dropped entry under backpressure.");
        Assert.True(snapshot.WrittenEntries >= 1, "At least one entry should be written.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    private class SlowTarget : ITarget
    {
        public int Writes;
        public void Write(LogEntry logEntry)
        {
            Writes++;
            Thread.Sleep(100); // slow to create backpressure
        }

        public void Close() { }
    }
}

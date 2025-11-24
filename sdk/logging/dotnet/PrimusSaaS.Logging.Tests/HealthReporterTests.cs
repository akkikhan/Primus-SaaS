using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Health;
using PrimusSaaS.Logging.Targets;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class HealthReporterTests
{
    [Fact]
    public void Snapshot_ShouldSurfaceMetricsAndTargets()
    {
        var metrics = new LoggingMetrics();
        var targets = new List<ITarget> { new ConsoleTarget() };
        var reporter = new LoggingHealthReporter(metrics, targets);

        metrics.IncrementWritten();
        metrics.IncrementDropped();
        var snapshot = reporter.Snapshot();

        Assert.Equal(1, snapshot.Metrics.WrittenEntries);
        Assert.Equal(1, snapshot.Metrics.DroppedEntries);
        Assert.Single(snapshot.Targets);
        Assert.Equal("ConsoleTarget", snapshot.Targets[0].Name);
    }
}

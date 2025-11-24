using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;

namespace PrimusSaaS.Logging.Health;

/// <summary>
/// Provides lightweight health snapshots for logging (metrics + target statuses).
/// </summary>
public class LoggingHealthReporter
{
    private readonly LoggingMetrics _metrics;
    private readonly IReadOnlyList<ITarget> _targets;

    public LoggingHealthReporter(LoggingMetrics metrics, IReadOnlyList<ITarget> targets)
    {
        _metrics = metrics;
        _targets = targets;
    }

    public LoggingHealthSnapshot Snapshot()
    {
        // For now, targets are assumed healthy if constructed; deeper checks can be added per target type.
        var targetStatuses = _targets.Select(t => new TargetHealth(t.GetType().Name, true, null)).ToList();
        return new LoggingHealthSnapshot(_metrics.Snapshot(), targetStatuses);
    }
}

public record LoggingHealthSnapshot(LoggingMetricsSnapshot Metrics, IReadOnlyList<TargetHealth> Targets);

public record TargetHealth(string Name, bool Healthy, string? Detail);

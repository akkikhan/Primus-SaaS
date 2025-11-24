namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Tracks counts of security events (auth successes/failures/rate-limited).
/// </summary>
public class SecurityEventMetrics
{
    private long _successes;
    private long _failures;
    private long _rateLimited;

    public void IncrementSuccess() => Interlocked.Increment(ref _successes);
    public void IncrementFailure() => Interlocked.Increment(ref _failures);
    public void IncrementRateLimited() => Interlocked.Increment(ref _rateLimited);

    public SecurityEventMetricsSnapshot Snapshot() => new()
    {
        AuthSuccesses = Interlocked.Read(ref _successes),
        AuthFailures = Interlocked.Read(ref _failures),
        RateLimited = Interlocked.Read(ref _rateLimited)
    };
}

public class SecurityEventMetricsSnapshot
{
    public long AuthSuccesses { get; init; }
    public long AuthFailures { get; init; }
    public long RateLimited { get; init; }
}

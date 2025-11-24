namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Snapshot of JWKS fetch/cache diagnostics for observability and troubleshooting.
/// </summary>
public class JwksServiceDiagnostics
{
    /// <summary>
    /// Number of cache hits.
    /// </summary>
    public long CacheHits { get; init; }

    /// <summary>
    /// Number of cache misses.
    /// </summary>
    public long CacheMisses { get; init; }

    /// <summary>
    /// Total fetch attempts (including retries).
    /// </summary>
    public long FetchAttempts { get; init; }

    /// <summary>
    /// Number of failed fetch attempts.
    /// </summary>
    public long FetchFailures { get; init; }

    /// <summary>
    /// Timestamp of the last successful fetch (UTC), if any.
    /// </summary>
    public DateTimeOffset? LastSuccessUtc { get; init; }
}

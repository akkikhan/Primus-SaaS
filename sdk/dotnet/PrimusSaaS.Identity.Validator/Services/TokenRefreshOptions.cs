namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Options for token refresh support.
/// </summary>
public class TokenRefreshOptions
{
    /// <summary>
    /// Enable built-in token refresh service registration.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Use in-memory refresh token store (development/demo only).
    /// </summary>
    public bool UseInMemoryStore { get; set; }

    /// <summary>
    /// Access token lifetime when issued via refresh. Default: 1 hour.
    /// </summary>
    public TimeSpan AccessTokenTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Refresh token lifetime. Default: 30 days.
    /// </summary>
    public TimeSpan RefreshTokenTtl { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// When true, a durable store-backed refresh service will be used if available (default false).
    /// </summary>
    public bool UseDurableStore { get; set; }

    internal void Validate()
    {
        if (AccessTokenTtl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(AccessTokenTtl), "AccessTokenTtl must be greater than zero.");
        if (RefreshTokenTtl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(RefreshTokenTtl), "RefreshTokenTtl must be greater than zero.");
    }
}

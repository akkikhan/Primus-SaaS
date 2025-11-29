namespace PrimusSaaS.Identity.Validator.Services;

public class TenantRateLimitOptions
{
    /// <summary>
    /// Enable per-tenant rate limiting.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Max requests per window for a tenant/API key.
    /// </summary>
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    /// Window length in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}

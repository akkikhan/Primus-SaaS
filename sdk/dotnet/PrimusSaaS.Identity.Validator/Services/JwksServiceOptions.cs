namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Options to control JWKS fetching resiliency.
/// </summary>
public class JwksServiceOptions
{
    /// <summary>
    /// Maximum number of attempts when fetching JWKS. Default: 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay for retry backoff. Default: 200ms.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    internal void Validate()
    {
        if (MaxRetries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRetries), "MaxRetries must be greater than zero.");
        }

        if (BaseDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(BaseDelay), "BaseDelay cannot be negative.");
        }
    }
}

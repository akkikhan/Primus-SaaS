namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Options for rate limiting repeated failed token validations.
/// </summary>
public class FailedValidationRateLimiterOptions
{
    /// <summary>
    /// Enable or disable rate limiting. Default: false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum failures allowed per window per client key (IP). Default: 20.
    /// </summary>
    public int MaxFailuresPerWindow { get; set; } = 20;

    /// <summary>
    /// Optional global ceiling per window. Set to 0 to disable global limit. Default: 0 (disabled).
    /// </summary>
    public int MaxGlobalFailuresPerWindow { get; set; }

    /// <summary>
    /// Sliding window duration for counting failures. Default: 1 minute.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    internal void Validate()
    {
        if (MaxFailuresPerWindow <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxFailuresPerWindow), "MaxFailuresPerWindow must be greater than zero.");
        if (MaxGlobalFailuresPerWindow < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxGlobalFailuresPerWindow), "MaxGlobalFailuresPerWindow cannot be negative.");
        if (Window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Window), "Window must be greater than zero.");
    }
}

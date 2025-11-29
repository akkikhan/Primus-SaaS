namespace PrimusSaaS.Notifications.Configuration;

public class SmsOptions
{
    /// <summary>
    /// Sender identifier (if supported by provider).
    /// </summary>
    public string From { get; set; } = "Primus";

    /// <summary>
    /// Maximum retry attempts for transient failures.
    /// </summary>
    public int MaxRetryCount { get; set; } = 1;

    /// <summary>
    /// Base delay (ms) for exponential backoff between retries.
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 250;

    /// <summary>
    /// Optional timeout in seconds for outbound SMS requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    public void Validate()
    {
        if (MaxRetryCount < 0)
        {
            throw new ArgumentException("MaxRetryCount must be >= 0.", nameof(MaxRetryCount));
        }

        if (RetryBaseDelayMs <= 0)
        {
            throw new ArgumentException("RetryBaseDelayMs must be > 0.", nameof(RetryBaseDelayMs));
        }

        if (TimeoutSeconds <= 0)
        {
            throw new ArgumentException("TimeoutSeconds must be > 0.", nameof(TimeoutSeconds));
        }
    }
}

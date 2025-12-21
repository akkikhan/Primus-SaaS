namespace PrimusSaaS.Notifications.Configuration;

public class RedisRateLimitOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = "primus:notifications:ratelimit";
    public int KeyExpiryPaddingSeconds { get; set; } = 5;
}

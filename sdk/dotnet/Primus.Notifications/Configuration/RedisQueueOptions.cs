namespace PrimusSaaS.Notifications.Configuration;

/// <summary>
/// Redis-backed persistent queue configuration.
/// </summary>
public class RedisQueueOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueKey { get; set; } = "primus:notifications";
    public int PollIntervalMs { get; set; } = 250;
}

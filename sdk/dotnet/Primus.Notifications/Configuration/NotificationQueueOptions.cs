namespace Primus.Notifications.Configuration;

public class NotificationQueueOptions
{
    public int BoundedCapacity { get; set; } = 1000;
    public int MaxParallelHandlers { get; set; } = 2;
    public int MaxRetryCount { get; set; } = 2;
    public int BaseRetryDelayMs { get; set; } = 200;
}

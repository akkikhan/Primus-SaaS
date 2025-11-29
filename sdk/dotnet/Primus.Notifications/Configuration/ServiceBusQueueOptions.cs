namespace PrimusSaaS.Notifications.Configuration;

/// <summary>
/// Azure Service Bus-backed persistent queue configuration.
/// </summary>
public class ServiceBusQueueOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = "primus-notifications";
    public int PrefetchCount { get; set; } = 10;
    public int ReceiveWaitTimeSeconds { get; set; } = 5;
}

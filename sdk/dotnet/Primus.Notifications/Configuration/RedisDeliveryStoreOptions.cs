namespace PrimusSaaS.Notifications.Configuration;

public class RedisDeliveryStoreOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ListKey { get; set; } = "primus:notifications:delivery";
    public int MaxRecords { get; set; } = 10000;
}

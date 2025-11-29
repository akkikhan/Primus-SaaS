namespace PrimusSaaS.Notifications.Configuration;

public class WebhookOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool SendOnSuccess { get; set; } = true;
    public bool SendOnFailure { get; set; } = true;
}

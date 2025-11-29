namespace PrimusSaaS.Notifications.Configuration;

public class SendGridOptions
{
    public const string SectionName = "SendGrid";
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Primus Notifications";
    public bool ValidateOnStartup { get; set; } = true;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(FromAddress);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("SendGrid ApiKey is required.");
        }
        if (string.IsNullOrWhiteSpace(FromAddress))
        {
            throw new InvalidOperationException("SendGrid FromAddress is required.");
        }
    }
}

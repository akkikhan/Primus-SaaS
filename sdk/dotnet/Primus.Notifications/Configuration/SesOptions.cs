namespace PrimusSaaS.Notifications.Configuration;

public class SesOptions
{
    public const string SectionName = "Ses";
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string Region { get; set; } = "us-east-1";
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Primus Notifications";
    public bool ValidateOnStartup { get; set; } = true;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(FromAddress);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(FromAddress))
        {
            throw new InvalidOperationException("SES FromAddress is required.");
        }
        if (string.IsNullOrWhiteSpace(Region))
        {
            throw new InvalidOperationException("SES Region is required.");
        }
    }
}

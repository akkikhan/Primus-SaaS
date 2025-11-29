namespace PrimusSaaS.Notifications.Configuration;

/// <summary>
/// Configuration options for AWS Simple Notification Service (SNS) SMS provider.
/// </summary>
public class AwsSnsOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AwsSns";

    /// <summary>
    /// AWS Access Key ID.
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// AWS Secret Access Key.
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// AWS Region for SNS (e.g., "us-east-1", "eu-west-1").
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Optional: Sender ID (alphanumeric string, max 11 characters) displayed on recipient device.
    /// Note: Not supported in all countries (e.g., not in US).
    /// </summary>
    public string? SenderId { get; set; }

    /// <summary>
    /// SMS message type: "Promotional" or "Transactional".
    /// Transactional messages are higher priority and used for time-sensitive content.
    /// Default: "Transactional"
    /// </summary>
    public string SmsType { get; set; } = "Transactional";

    /// <summary>
    /// Whether to validate configuration during service startup (default: true).
    /// </summary>
    public bool ValidateOnStartup { get; set; } = true;

    /// <summary>
    /// Indicates whether any AWS credentials have been provided.
    /// Used to skip validation when the provider is not configured.
    /// </summary>
    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(AccessKeyId)
        || !string.IsNullOrWhiteSpace(SecretAccessKey);

    /// <summary>
    /// Validates the AWS SNS configuration.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AccessKeyId))
        {
            throw new InvalidOperationException(
                "AWS AccessKeyId is required.\n" +
                "Set 'AwsSns:AccessKeyId' in appsettings.json or environment variable 'AwsSns__AccessKeyId'.\n" +
                "Create access keys at: https://console.aws.amazon.com/iam/");
        }

        if (string.IsNullOrWhiteSpace(SecretAccessKey))
        {
            throw new InvalidOperationException(
                "AWS SecretAccessKey is required.\n" +
                "Set 'AwsSns:SecretAccessKey' in appsettings.json or environment variable 'AwsSns__SecretAccessKey'.\n" +
                "Create access keys at: https://console.aws.amazon.com/iam/");
        }

        if (string.IsNullOrWhiteSpace(Region))
        {
            throw new InvalidOperationException(
                "AWS Region is required.\n" +
                "Set 'AwsSns:Region' in appsettings.json (e.g., 'us-east-1').\n" +
                "See available regions: https://docs.aws.amazon.com/general/latest/gr/sns.html");
        }

        if (!string.IsNullOrWhiteSpace(SmsType) && 
            SmsType != "Promotional" && SmsType != "Transactional")
        {
            throw new InvalidOperationException(
                $"AWS SNS SmsType '{SmsType}' is invalid. Use 'Promotional' or 'Transactional'.");
        }

        if (!string.IsNullOrWhiteSpace(SenderId) && SenderId.Length > 11)
        {
            throw new InvalidOperationException(
                $"AWS SNS SenderId '{SenderId}' exceeds maximum length of 11 characters.");
        }
    }
}

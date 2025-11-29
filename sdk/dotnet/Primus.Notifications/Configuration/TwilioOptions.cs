namespace PrimusSaaS.Notifications.Configuration;

/// <summary>
/// Configuration options for Twilio SMS provider.
/// </summary>
public class TwilioOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Twilio";

    /// <summary>
    /// Your Twilio Account SID (starts with AC).
    /// </summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Your Twilio Auth Token.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// The Twilio phone number to send SMS from (E.164 format, e.g., +16205538468).
    /// </summary>
    public string FromNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Messaging Service SID (use instead of FromNumber for advanced routing).
    /// </summary>
    public string? MessagingServiceSid { get; set; }

    /// <summary>
    /// Whether to validate configuration during service startup (default: true).
    /// </summary>
    public bool ValidateOnStartup { get; set; } = true;

    /// <summary>
    /// Indicates whether any Twilio credentials/identifiers have been provided.
    /// Used to skip validation when the provider is not configured (e.g., email-only deployments).
    /// </summary>
    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(AccountSid)
        || !string.IsNullOrWhiteSpace(AuthToken)
        || !string.IsNullOrWhiteSpace(FromNumber)
        || !string.IsNullOrWhiteSpace(MessagingServiceSid);

    /// <summary>
    /// Validates the Twilio configuration.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AccountSid))
        {
            throw new InvalidOperationException(
                "Twilio AccountSid is required.\n" +
                "Set 'Twilio:AccountSid' in appsettings.json or environment variable 'Twilio__AccountSid'.\n" +
                "Find your Account SID at: https://www.twilio.com/console");
        }

        if (!AccountSid.StartsWith("AC", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Twilio AccountSid '{AccountSid}' is invalid. It should start with 'AC'.\n" +
                "Find your Account SID at: https://www.twilio.com/console");
        }

        if (string.IsNullOrWhiteSpace(AuthToken))
        {
            throw new InvalidOperationException(
                "Twilio AuthToken is required.\n" +
                "Set 'Twilio:AuthToken' in appsettings.json or environment variable 'Twilio__AuthToken'.\n" +
                "Find your Auth Token at: https://www.twilio.com/console");
        }

        if (string.IsNullOrWhiteSpace(FromNumber) && string.IsNullOrWhiteSpace(MessagingServiceSid))
        {
            throw new InvalidOperationException(
                "Either Twilio FromNumber or MessagingServiceSid is required.\n" +
                "Set 'Twilio:FromNumber' to your Twilio phone number (e.g., +16205538468).\n" +
                "Get a phone number at: https://www.twilio.com/console/phone-numbers");
        }

        if (!string.IsNullOrWhiteSpace(FromNumber) && !FromNumber.StartsWith("+", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Twilio FromNumber '{FromNumber}' should be in E.164 format (e.g., +16205538468).");
        }
    }
}

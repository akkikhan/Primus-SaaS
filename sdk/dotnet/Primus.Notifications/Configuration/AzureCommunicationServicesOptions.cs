namespace PrimusSaaS.Notifications.Configuration;

/// <summary>
/// Configuration options for Azure Communication Services SMS provider.
/// </summary>
public class AzureCommunicationServicesOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AzureCommunicationServices";

    /// <summary>
    /// Azure Communication Services connection string.
    /// Found in Azure Portal: Communication Service resource → Keys → Connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// When true, use managed identity (DefaultAzureCredential) instead of connection string.
    /// </summary>
    public bool UseManagedIdentity { get; set; }

    /// <summary>
    /// The ACS endpoint (required when UseManagedIdentity is true), e.g., https://xxx.communication.azure.com
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// The phone number to send SMS from (E.164 format, e.g., +18001234567).
    /// Must be a phone number provisioned in your Azure Communication Services resource.
    /// </summary>
    public string FromNumber { get; set; } = string.Empty;

    /// <summary>
    /// Whether to enable delivery reports for sent messages (default: true).
    /// </summary>
    public bool EnableDeliveryReport { get; set; } = true;

    /// <summary>
    /// Optional tag for tracking SMS in Azure Communication Services analytics.
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// Whether to validate configuration during service startup (default: true).
    /// </summary>
    public bool ValidateOnStartup { get; set; } = true;

    /// <summary>
    /// Indicates whether any Azure Communication Services configuration has been provided.
    /// Used to skip validation when the provider is not configured.
    /// </summary>
    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ConnectionString)
        || UseManagedIdentity
        || !string.IsNullOrWhiteSpace(FromNumber);

    /// <summary>
    /// Validates the Azure Communication Services configuration.
    /// </summary>
    public void Validate()
    {
        if (!UseManagedIdentity && string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                "Azure Communication Services ConnectionString is required.\n" +
                "Set 'AzureCommunicationServices:ConnectionString' in appsettings.json.\n" +
                "Find it in Azure Portal: Communication Services → Keys → Connection string.");
        }

        if (!UseManagedIdentity && !ConnectionString.Contains("endpoint=", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Azure Communication Services ConnectionString appears to be invalid.\n" +
                "It should contain 'endpoint=' and look like: 'endpoint=https://xxx.communication.azure.com/;accesskey=...'");
        }

        if (UseManagedIdentity && string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new InvalidOperationException(
                "Azure Communication Services Endpoint is required when UseManagedIdentity=true.\n" +
                "Example: https://xxx.communication.azure.com");
        }

        if (string.IsNullOrWhiteSpace(FromNumber))
        {
            throw new InvalidOperationException(
                "Azure Communication Services FromNumber is required.\n" +
                "Set 'AzureCommunicationServices:FromNumber' to your provisioned phone number (e.g., +18001234567).\n" +
                "Get a phone number at: Azure Portal → Communication Services → Phone numbers.");
        }

        if (!FromNumber.StartsWith("+", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Azure Communication Services FromNumber '{FromNumber}' should be in E.164 format (e.g., +18001234567).");
        }
    }
}

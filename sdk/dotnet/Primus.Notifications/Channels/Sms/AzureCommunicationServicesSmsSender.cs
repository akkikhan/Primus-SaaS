using System.Text.RegularExpressions;
using Azure;
using Azure.Communication.Sms;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;

namespace PrimusSaaS.Notifications.Channels.Sms;

/// <summary>
/// SMS sender implementation using Azure Communication Services.
/// </summary>
public sealed class AzureCommunicationServicesSmsSender : ISmsSender
{
    private readonly SmsClient _smsClient;
    private readonly AzureCommunicationServicesOptions _options;
    private readonly ILogger<AzureCommunicationServicesSmsSender> _logger;

    /// <summary>
    /// Creates a new Azure Communication Services SMS sender instance.
    /// </summary>
    public AzureCommunicationServicesSmsSender(
        IOptions<AzureCommunicationServicesOptions> options,
        ILogger<AzureCommunicationServicesSmsSender> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_options.ValidateOnStartup && _options.IsConfigured())
        {
            _options.Validate();
        }

        // Create Azure Communication Services SMS client
        if (_options.UseManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(_options.Endpoint))
            {
                throw new InvalidOperationException("Endpoint is required when UseManagedIdentity=true.");
            }
            _smsClient = new SmsClient(new Uri(_options.Endpoint), new DefaultAzureCredential());
        }
        else
        {
            _smsClient = new SmsClient(_options.ConnectionString);
        }

        _logger.LogDebug(
            "Azure Communication Services SMS sender initialized with from number {FromNumber}",
            MaskPhoneNumber(_options.FromNumber));
    }

    /// <inheritdoc/>
    public async Task SendAsync(
        string to,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("Recipient phone number is required.", nameof(to));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message content is required.", nameof(message));
        }

        var normalizedNumber = NormalizePhoneNumber(to);

        _logger.LogInformation(
            "Sending SMS via Azure Communication Services from {From} to {To} ({Length} chars)",
            MaskPhoneNumber(_options.FromNumber),
            MaskPhoneNumber(normalizedNumber),
            message.Length);

        try
        {
            var sendOptions = new SmsSendOptions(_options.EnableDeliveryReport);

            if (!string.IsNullOrWhiteSpace(_options.Tag))
            {
                sendOptions.Tag = _options.Tag;
            }

            var response = await _smsClient.SendAsync(
                from: _options.FromNumber,
                to: normalizedNumber,
                message: message,
                options: sendOptions,
                cancellationToken: cancellationToken);

            var result = response.Value;

            if (result.Successful)
            {
                _logger.LogInformation(
                    "Azure Communication Services SMS sent successfully to {To}. MessageId: {MessageId}",
                    MaskPhoneNumber(normalizedNumber),
                    result.MessageId);
            }
            else
            {
                _logger.LogError(
                    "Azure Communication Services SMS failed to {To}. ErrorCode: {ErrorCode}, Message: {ErrorMessage}",
                    MaskPhoneNumber(normalizedNumber),
                    result.ErrorMessage,
                    result.ErrorMessage);

                throw new InvalidOperationException(
                    $"Azure SMS failed: {result.ErrorMessage}\n" +
                    GetErrorHelp(result.ErrorMessage ?? ""));
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Azure Communication Services error sending SMS to {To}: {ErrorCode} - {Message}",
                MaskPhoneNumber(normalizedNumber),
                ex.ErrorCode,
                ex.Message);

            throw new InvalidOperationException(
                $"Azure SMS failed: [{ex.ErrorCode}] {ex.Message}\n" +
                GetAzureErrorHelp(ex.ErrorCode),
                ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(
                ex,
                "Unexpected error sending SMS via Azure Communication Services to {To}",
                MaskPhoneNumber(normalizedNumber));
            throw;
        }
    }

    /// <summary>
    /// Normalizes a phone number to E.164 format for Azure Communication Services.
    /// </summary>
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        // Remove all non-digit characters except leading +
        var digits = Regex.Replace(phoneNumber, @"[^\d+]", "");

        // Ensure E.164 format with leading +
        if (!digits.StartsWith("+", StringComparison.Ordinal))
        {
            // Assume US number if 10 digits without country code
            if (digits.Length == 10)
            {
                digits = "+1" + digits;
            }
            else if (digits.Length == 11 && digits.StartsWith("1", StringComparison.Ordinal))
            {
                digits = "+" + digits;
            }
            else
            {
                digits = "+" + digits;
            }
        }

        return digits;
    }

    /// <summary>
    /// Masks a phone number for safe logging.
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 7)
        {
            return "***";
        }

        return phoneNumber[..4] + "***" + phoneNumber[^3..];
    }

    /// <summary>
    /// Provides helpful error resolution information based on error message.
    /// </summary>
    private static string GetErrorHelp(string errorMessage)
    {
        var lowerMessage = errorMessage.ToLowerInvariant();

        if (lowerMessage.Contains("invalid") && lowerMessage.Contains("phone"))
        {
            return "Ensure the phone number is in E.164 format (e.g., +14155551234).";
        }

        if (lowerMessage.Contains("not provisioned") || lowerMessage.Contains("from"))
        {
            return "The 'from' phone number is not provisioned in your Azure Communication Services resource.\n" +
                   "Get a phone number at: Azure Portal → Communication Services → Phone numbers.";
        }

        return "See Azure Communication Services SMS documentation:\n" +
               "https://docs.microsoft.com/azure/communication-services/concepts/sms/sms-faq";
    }

    /// <summary>
    /// Provides helpful error resolution information based on Azure error code.
    /// </summary>
    private static string GetAzureErrorHelp(string? errorCode) => errorCode switch
    {
        "Unauthorized" or "401" =>
            "Authentication failed. Check your connection string is correct.\n" +
            "Find it in Azure Portal: Communication Services → Keys → Connection string.",

        "Forbidden" or "403" =>
            "Access denied. Ensure your Azure Communication Services resource allows SMS.\n" +
            "Check that you have a phone number with SMS capabilities enabled.",

        "NotFound" or "404" =>
            "Resource not found. Verify your Azure Communication Services resource exists\n" +
            "and the connection string endpoint is correct.",

        "TooManyRequests" or "429" =>
            "Rate limit exceeded. Azure Communication Services has sending limits.\n" +
            "Wait and retry, or contact Azure support for higher limits.",

        "InvalidPhoneNumber" =>
            "The phone number format is invalid. Use E.164 format (e.g., +14155551234).",

        "MessageBodyRequired" =>
            "The message body cannot be empty.",

        "MessageTooLong" =>
            "The message exceeds the maximum allowed length (1600 characters for SMS).",

        _ => "See Azure Communication Services troubleshooting:\n" +
             "https://docs.microsoft.com/azure/communication-services/concepts/troubleshooting-info"
    };
}

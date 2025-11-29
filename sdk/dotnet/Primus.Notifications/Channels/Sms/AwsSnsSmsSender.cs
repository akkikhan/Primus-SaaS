using System.Text.RegularExpressions;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;

namespace PrimusSaaS.Notifications.Channels.Sms;

/// <summary>
/// SMS sender implementation using AWS Simple Notification Service (SNS).
/// </summary>
public sealed class AwsSnsSmsSender : ISmsSender, IDisposable
{
    private readonly AmazonSimpleNotificationServiceClient _snsClient;
    private readonly AwsSnsOptions _options;
    private readonly ILogger<AwsSnsSmsSender> _logger;
    private bool _disposed;

    /// <summary>
    /// Creates a new AWS SNS SMS sender instance.
    /// </summary>
    public AwsSnsSmsSender(
        IOptions<AwsSnsOptions> options,
        ILogger<AwsSnsSmsSender> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_options.ValidateOnStartup && _options.IsConfigured())
        {
            _options.Validate();
        }

        // Create SNS client with credentials
        var region = RegionEndpoint.GetBySystemName(_options.Region);
        _snsClient = new AmazonSimpleNotificationServiceClient(
            _options.AccessKeyId,
            _options.SecretAccessKey,
            region);

        _logger.LogDebug(
            "AWS SNS SMS sender initialized for region {Region}",
            _options.Region);
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
            "Sending SMS via AWS SNS to {To} ({Length} chars)",
            MaskPhoneNumber(normalizedNumber),
            message.Length);

        try
        {
            var request = new PublishRequest
            {
                PhoneNumber = normalizedNumber,
                Message = message,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["AWS.SNS.SMS.SMSType"] = new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = _options.SmsType
                    }
                }
            };

            // Add optional SenderId if configured and supported
            if (!string.IsNullOrWhiteSpace(_options.SenderId))
            {
                request.MessageAttributes["AWS.SNS.SMS.SenderID"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = _options.SenderId
                };
            }

            var response = await _snsClient.PublishAsync(request, cancellationToken);

            _logger.LogInformation(
                "AWS SNS SMS sent successfully to {To}. MessageId: {MessageId}",
                MaskPhoneNumber(normalizedNumber),
                response.MessageId);
        }
        catch (AmazonSimpleNotificationServiceException ex)
        {
            _logger.LogError(
                ex,
                "AWS SNS error sending SMS to {To}: {ErrorCode} - {Message}",
                MaskPhoneNumber(normalizedNumber),
                ex.ErrorCode,
                ex.Message);

            throw new InvalidOperationException(
                $"AWS SNS SMS failed: [{ex.ErrorCode}] {ex.Message}\n" +
                GetErrorHelp(ex.ErrorCode),
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error sending SMS via AWS SNS to {To}",
                MaskPhoneNumber(normalizedNumber));
            throw;
        }
    }

    /// <summary>
    /// Normalizes a phone number to E.164 format for AWS SNS.
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
    /// Provides helpful error resolution information based on AWS error code.
    /// </summary>
    private static string GetErrorHelp(string errorCode) => errorCode switch
    {
        "InvalidParameter" =>
            "Check that the phone number is in E.164 format (e.g., +14155551234).",

        "AuthorizationError" =>
            "AWS credentials may be invalid or lack SNS SMS permissions.\n" +
            "Ensure the IAM user/role has 'sns:Publish' permission.",

        "Throttling" =>
            "SMS sending rate exceeded. AWS SNS has default limits.\n" +
            "Request a limit increase: https://console.aws.amazon.com/servicequotas/",

        "OptedOut" =>
            "The recipient has opted out of receiving SMS messages from your AWS account.",

        "InternalError" =>
            "AWS internal error. Retry the request after a brief delay.",

        _ => $"See AWS SNS documentation: https://docs.aws.amazon.com/sns/latest/dg/sms_publish-to-phone.html"
    };

    /// <summary>
    /// Releases unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _snsClient?.Dispose();
            _disposed = true;
        }
    }
}

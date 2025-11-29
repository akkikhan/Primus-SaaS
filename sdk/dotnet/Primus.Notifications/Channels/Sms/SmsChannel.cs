using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Core;

namespace PrimusSaaS.Notifications.Channels.Sms;

public class SmsChannel : IChannel
{
    public string Name => "Sms";

    private readonly ISmsSender _smsSender;
    private readonly ITemplateService _templateService;
    private readonly SmsOptions _options;
    private readonly ILogger<SmsChannel> _logger;

    public SmsChannel(
        ISmsSender smsSender,
        ITemplateService templateService,
        IOptions<SmsOptions> options,
        ILogger<SmsChannel> logger)
    {
        _smsSender = smsSender;
        _templateService = templateService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notification.Recipient.PhoneNumber))
        {
            _logger.LogWarning("SMS channel requested but recipient has no phone number.");
            return;
        }

        var direct = notification.Data as DirectSmsContent;
        var isDirect = notification.Type.Equals(BasicNotification.DirectSmsType, StringComparison.OrdinalIgnoreCase);
        string message;

        if (isDirect)
        {
            message = direct?.Message ?? TryGetProperty(notification.Data, "Message") ?? notification.Data.ToString() ?? string.Empty;
        }
        else
        {
            var renderedBody = await _templateService.RenderAsync(notification.Type, "SmsBody", notification.Data);
            message = string.IsNullOrWhiteSpace(renderedBody)
                ? direct?.Message ?? TryGetProperty(notification.Data, "Message") ?? notification.Data.ToString() ?? string.Empty
                : renderedBody;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("SMS message is empty for notification type {Type}. Skipping send.", notification.Type);
            return;
        }

        for (var attempt = 0; attempt <= _options.MaxRetryCount; attempt++)
        {
            try
            {
                await _smsSender.SendAsync(notification.Recipient.PhoneNumber, message, cancellationToken);
                _logger.LogInformation("Successfully sent SMS to {Phone}", notification.Recipient.PhoneNumber);
                return;
            }
            catch (Exception ex)
            {
                var attemptNumber = attempt + 1;
                var maxAttempts = _options.MaxRetryCount + 1;
                _logger.LogError(ex, "SMS send failed (attempt {Attempt}/{MaxAttempts})", attemptNumber, maxAttempts);

                var shouldRetry = attempt < _options.MaxRetryCount && !cancellationToken.IsCancellationRequested;
                if (!shouldRetry)
                {
                    throw;
                }

                var delayMs = _options.RetryBaseDelayMs * (int)Math.Pow(2, attempt);
                try
                {
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private static string? TryGetProperty(object data, string propertyName)
    {
        if (data == null) return null;
        var prop = data.GetType().GetProperty(propertyName);
        if (prop == null || prop.GetIndexParameters().Length > 0) return null;
        var value = prop.GetValue(data);
        return value?.ToString();
    }
}

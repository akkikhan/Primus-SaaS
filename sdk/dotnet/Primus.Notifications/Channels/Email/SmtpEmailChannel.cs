using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Primus.Notifications.Abstractions;
using Primus.Notifications.Configuration;

namespace Primus.Notifications.Channels.Email;

public class SmtpEmailChannel : IChannel
{
    public string Name => "Email";

    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailChannel> _logger;
    private readonly ITemplateService _templateService;

    public SmtpEmailChannel(IOptions<SmtpOptions> options, ILogger<SmtpEmailChannel> logger, ITemplateService templateService)
    {
        _options = options.Value;
        _logger = logger;
        _templateService = templateService;
    }

    public async Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(notification.Recipient.Email))
        {
            _logger.LogWarning("Email channel requested but recipient has no email address.");
            return;
        }

        var subject = await _templateService.RenderAsync(notification.Type, "EmailSubject", notification.Data);
        var body = await _templateService.RenderAsync(notification.Type, "EmailBody", notification.Data);

        // Fallback if template returns empty (maybe data has it directly?)
        if (string.IsNullOrEmpty(subject)) subject = "Notification";
        if (string.IsNullOrEmpty(body)) body = notification.Data.ToString() ?? "";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(notification.Recipient.Name, notification.Recipient.Email));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body
        };
        message.Body = bodyBuilder.ToMessageBody();

        var secureSocket = _options.EnableSsl ? MailKit.Security.SecureSocketOptions.Auto : MailKit.Security.SecureSocketOptions.None;

        for (var attempt = 0; attempt <= _options.MaxRetryCount; attempt++)
        {
            using var client = new SmtpClient { Timeout = _options.TimeoutSeconds * 1000 };
            try
            {
                await client.ConnectAsync(_options.Host, _options.Port, secureSocket, cancellationToken);
                
                if (!string.IsNullOrEmpty(_options.Username))
                {
                    await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                var attemptNumber = attempt + 1;
                var maxAttempts = _options.MaxRetryCount + 1;
                _logger.LogError(ex, "SMTP send failed (attempt {Attempt}/{MaxAttempts})", attemptNumber, maxAttempts);

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
}

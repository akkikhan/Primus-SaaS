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

        using var client = new SmtpClient();
        try
        {
            // Use Auto to negotiate the best security (StartTLS for 587, SSL/TLS for 465)
            await client.ConnectAsync(_options.Host, _options.Port, MailKit.Security.SecureSocketOptions.Auto, cancellationToken);
            
            if (!string.IsNullOrEmpty(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP Send Failed");
            throw;
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Core;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PrimusSaaS.Notifications.Channels.Email;

public class SendGridEmailChannel : IChannel
{
    public string Name => "Email";

    private readonly SendGridOptions _options;
    private readonly ITemplateService _templateService;
    private readonly ILogger<SendGridEmailChannel> _logger;
    private readonly ISendGridClient _client;

    public SendGridEmailChannel(
        IOptions<SendGridOptions> options,
        ITemplateService templateService,
        ILogger<SendGridEmailChannel> logger,
        ISendGridClient client)
    {
        _options = options.Value ?? new SendGridOptions();
        _templateService = templateService;
        _logger = logger;
        _client = client;
    }

    public async Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notification.Recipient.Email))
        {
            _logger.LogWarning("SendGrid channel requested but recipient has no email.");
            return;
        }

        var directContent = notification.Data as DirectEmailContent;
        var isDirect = notification.Type.Equals(BasicNotification.DirectEmailType, StringComparison.OrdinalIgnoreCase);

        string subject;
        string body;

        if (isDirect)
        {
            subject = directContent?.Subject ?? "Notification";
            body = directContent?.Body ?? notification.Data.ToString() ?? string.Empty;
        }
        else
        {
            var renderedSubject = await _templateService.RenderAsync(notification.Type, "EmailSubject", notification.Data);
            var renderedBody = await _templateService.RenderAsync(notification.Type, "EmailBody", notification.Data);
            subject = string.IsNullOrWhiteSpace(renderedSubject) ? "Notification" : renderedSubject;
            body = string.IsNullOrWhiteSpace(renderedBody) ? notification.Data.ToString() ?? string.Empty : renderedBody;
        }

        var from = new EmailAddress(_options.FromAddress, _options.FromName);
        var to = new EmailAddress(notification.Recipient.Email, notification.Recipient.Name);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: body);

        var response = await _client.SendEmailAsync(msg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Body.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"SendGrid send failed: {response.StatusCode} {text}");
        }
    }
}

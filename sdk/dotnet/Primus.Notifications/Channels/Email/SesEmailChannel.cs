using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Core;

namespace PrimusSaaS.Notifications.Channels.Email;

public class SesEmailChannel : IChannel
{
    public string Name => "Email";

    private readonly SesOptions _options;
    private readonly ITemplateService _templateService;
    private readonly ILogger<SesEmailChannel> _logger;
    private readonly IAmazonSimpleEmailService _client;

    public SesEmailChannel(
        IOptions<SesOptions> options,
        ITemplateService templateService,
        ILogger<SesEmailChannel> logger)
    {
        _options = options.Value ?? new SesOptions();
        _templateService = templateService;
        _logger = logger;
        _client = BuildClient(_options);
    }

    public async Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notification.Recipient.Email))
        {
            _logger.LogWarning("SES channel requested but recipient has no email.");
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

        var request = new SendEmailRequest
        {
            Source = $"{_options.FromName} <{_options.FromAddress}>",
            Destination = new Destination
            {
                ToAddresses = new() { notification.Recipient.Email }
            },
            Message = new Message
            {
                Subject = new Content(subject),
                Body = new Body
                {
                    Html = new Content(body)
                }
            }
        };

        var response = await _client.SendEmailAsync(request, cancellationToken);
        if (response.HttpStatusCode is not System.Net.HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"SES send failed: {response.HttpStatusCode} {response.MessageId}");
        }
    }

    private static IAmazonSimpleEmailService BuildClient(SesOptions options)
    {
        AWSCredentials credentials;
        if (!string.IsNullOrWhiteSpace(options.AccessKeyId) && !string.IsNullOrWhiteSpace(options.SecretAccessKey))
        {
            credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
        }
        else
        {
            credentials = FallbackCredentialsFactory.GetCredentials();
        }

        return new AmazonSimpleEmailServiceClient(credentials, RegionEndpoint.GetBySystemName(options.Region));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Primus.Notifications.Abstractions;
using Primus.Notifications.Diagnostics;

namespace Primus.Notifications.Core;

public class NotificationService
{
    private readonly IEnumerable<IChannel> _channels;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IEnumerable<IChannel> channels, ILogger<NotificationService> logger)
    {
        _channels = channels;
        _logger = logger;
    }

    public Task SendEmailAsync(
        string to,
        string subject,
        string body,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("Recipient email is required.", nameof(to));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Email subject is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Email body is required.", nameof(body));

        var notification = new BasicNotification(
            type: "primus.email.direct",
            data: new DirectEmailContent(subject, body),
            recipient: new Recipient { Email = to, Name = name ?? string.Empty },
            channels: new[] { "Email" });

        return SendAsync(notification, cancellationToken);
    }

    public Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Recipient phone number is required.", nameof(phoneNumber));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("SMS message is required.", nameof(message));

        var notification = new BasicNotification(
            type: "primus.sms.direct",
            data: new DirectSmsContent(message),
            recipient: new Recipient { PhoneNumber = phoneNumber },
            channels: new[] { "Sms" });

        return SendAsync(notification, cancellationToken);
    }

    public async Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting notification dispatch for {Type} to {Recipient}", notification.Type, notification.Recipient.Email ?? notification.Recipient.UserId);

        var tasks = new List<Task>();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        NotificationRuntimeStats.RecordQueued();

        foreach (var channelName in notification.Channels)
        {
            var channel = _channels.FirstOrDefault(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));
            if (channel == null)
            {
                _logger.LogWarning("Channel {ChannelName} requested but not registered.", channelName);
                continue;
            }

            tasks.Add(DispatchToChannelAsync(channel, notification, cancellationToken));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();
        NotificationMetrics.DispatchDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds);
        NotificationMetrics.NotificationsSent.Add(1);
        NotificationRuntimeStats.RecordSent(stopwatch.Elapsed.TotalMilliseconds);
        _logger.LogInformation("Notification dispatch complete.");
    }

    private async Task DispatchToChannelAsync(IChannel channel, INotification notification, CancellationToken token)
    {
        try
        {
            await channel.SendAsync(notification, token);
            _logger.LogInformation("Successfully sent notification via {Channel}", channel.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification via {Channel}", channel.Name);
            NotificationMetrics.NotificationsFailed.Add(1);
            NotificationRuntimeStats.RecordFailed();
            // We do not throw here to allow other channels to succeed
        }
    }
}

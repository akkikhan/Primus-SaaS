using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Diagnostics;

namespace PrimusSaaS.Notifications.Core;

/// <summary>
/// Core notification service that dispatches notifications through configured channels.
/// Implements <see cref="INotificationService"/> for dependency injection.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEnumerable<IChannel> _channels;
    private readonly ILogger<NotificationService> _logger;
    private readonly NotificationOptions _options;

    public NotificationService(IEnumerable<IChannel> channels, ILogger<NotificationService> logger)
        : this(channels, logger, Options.Create(new NotificationOptions()))
    {
    }

    public NotificationService(
        IEnumerable<IChannel> channels,
        ILogger<NotificationService> logger,
        IOptions<NotificationOptions> options)
    {
        _channels = channels;
        _logger = logger;
        _options = options?.Value ?? new NotificationOptions();
    }

    public Task<NotificationResult> SendEmailAsync(
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
            type: BasicNotification.DirectEmailType,
            data: new DirectEmailContent(subject, body),
            recipient: new Recipient { Email = to, Name = name ?? string.Empty },
            channels: new[] { "Email" });

        return SendAsync(notification, cancellationToken);
    }

    public Task<NotificationResult> SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Recipient phone number is required.", nameof(phoneNumber));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("SMS message is required.", nameof(message));

        var notification = new BasicNotification(
            type: BasicNotification.DirectSmsType,
            data: new DirectSmsContent(message),
            recipient: new Recipient { PhoneNumber = phoneNumber },
            channels: new[] { "Sms" });

        return SendAsync(notification, cancellationToken);
    }

    public async Task<NotificationResult> SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting notification dispatch for {Type} to {Recipient}", notification.Type, notification.Recipient.Email ?? notification.Recipient.UserId);

        var stopwatch = Stopwatch.StartNew();
        NotificationRuntimeStats.RecordQueued();

        var requestedChannels = notification.Channels?.ToArray() ?? Array.Empty<string>();
        var channelResults = new List<ChannelDispatchResult>();
        var dispatchTasks = new List<Task<ChannelDispatchResult>>();

        if (requestedChannels.Length == 0)
        {
            channelResults.Add(ChannelDispatchResult.Skipped("None", "No channels were requested for this notification."));
        }

        foreach (var channelName in requestedChannels)
        {
            var channel = _channels.FirstOrDefault(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));
            if (channel == null)
            {
                channelResults.Add(ChannelDispatchResult.Skipped(channelName, "Channel not registered."));
                _logger.LogWarning("Channel {ChannelName} requested but not registered.", channelName);
                continue;
            }

            dispatchTasks.Add(DispatchToChannelAsync(channel, notification, cancellationToken));
        }

        if (dispatchTasks.Count > 0)
        {
            var dispatched = await Task.WhenAll(dispatchTasks);
            channelResults.AddRange(dispatched);
        }

        stopwatch.Stop();
        NotificationMetrics.DispatchDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds);

        var firstSuccessChannel = channelResults.FirstOrDefault(r => r.Status == ChannelDispatchStatus.Sent)?.Channel;
        var success = firstSuccessChannel != null;
        var failureReason = success
            ? null
            : DetermineFailureReason(requestedChannels, channelResults);

        if (success)
        {
            NotificationMetrics.NotificationsSent.Add(1);
            NotificationRuntimeStats.RecordSent(stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogInformation("Notification dispatch complete via {Channel}.", firstSuccessChannel);
        }
        else
        {
            NotificationMetrics.NotificationsFailed.Add(1);
            NotificationRuntimeStats.RecordFailed();
            _logger.LogError("Notification dispatch failed: {Reason}", failureReason);
        }

        var result = NotificationResult.FromChannels(channelResults, failureReason);

        if (!success && _options.ThrowOnFailure)
        {
            throw new NotificationFailedException(result.FailureReason ?? "Notification failed", result);
        }

        return result;
    }

    private static string DetermineFailureReason(IEnumerable<string> requestedChannels, IEnumerable<ChannelDispatchResult> results)
    {
        if (!requestedChannels.Any())
        {
            return "No channels were requested.";
        }

        if (results.All(r => r.Status == ChannelDispatchStatus.Skipped))
        {
            return "No registered channels matched the request.";
        }

        return "All channels failed to send.";
    }

    private async Task<ChannelDispatchResult> DispatchToChannelAsync(IChannel channel, INotification notification, CancellationToken token)
    {
        try
        {
            await channel.SendAsync(notification, token);
            _logger.LogInformation("Successfully sent notification via {Channel}", channel.Name);
            return ChannelDispatchResult.Sent(channel.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification via {Channel}", channel.Name);
            return ChannelDispatchResult.Failed(channel.Name, ex);
        }
    }
}

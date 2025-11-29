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
using PrimusSaaS.Notifications.Channels.Sms;
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
    private readonly INotificationQueue? _queue;
    private readonly INotificationDeliveryStore? _deliveryStore;
    private readonly INotificationWebhookDispatcher? _webhookDispatcher;
    private readonly RateLimiter _rateLimiter = new();

    public NotificationService(IEnumerable<IChannel> channels, ILogger<NotificationService> logger)
        : this(channels, logger, Options.Create(new NotificationOptions()), serviceProvider: null)
    {
    }

    public NotificationService(
        IEnumerable<IChannel> channels,
        ILogger<NotificationService> logger,
        IOptions<NotificationOptions> options,
        IServiceProvider? serviceProvider = null,
        INotificationQueue? queue = null,
        INotificationDeliveryStore? deliveryStore = null,
        INotificationWebhookDispatcher? webhookDispatcher = null)
    {
        _channels = channels;
        _logger = logger;
        _options = options?.Value ?? new NotificationOptions();
        _options.RateLimit ??= new RateLimitOptions();
        _options.Webhooks ??= new WebhookOptions();
        _queue = queue ?? serviceProvider?.GetService(typeof(INotificationQueue)) as INotificationQueue;
        _deliveryStore = deliveryStore ?? serviceProvider?.GetService(typeof(INotificationDeliveryStore)) as INotificationDeliveryStore;
        _webhookDispatcher = webhookDispatcher ?? serviceProvider?.GetService(typeof(INotificationWebhookDispatcher)) as INotificationWebhookDispatcher;
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

    public async Task<NotificationResult> SendAsync(INotification notification, CancellationToken cancellationToken = default, bool fromQueue = false)
    {
        _logger.LogInformation("Starting notification dispatch for {Type} to {Recipient}", notification.Type, notification.Recipient.Email ?? notification.Recipient.UserId);

        var stopwatch = Stopwatch.StartNew();
        NotificationRuntimeStats.RecordQueued();

        if (_options.RateLimit.Enabled && !_rateLimiter.TryConsume(notification.Recipient, _options.RateLimit, out var rateLimitReason))
        {
            var rateLimitedResult = NotificationResult.FromChannels(
                new[] { ChannelDispatchResult.Skipped("RateLimit", rateLimitReason) },
                rateLimitReason,
                enqueuedForRetry: false,
                serviceUnavailable: false);

            await RecordDeliveryAsync(notification, rateLimitedResult, cancellationToken);
            return rateLimitedResult;
        }

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

        bool enqueuedForRetry = false;
        bool serviceUnavailable = false;

        if (success)
        {
            NotificationMetrics.NotificationsSent.Add(1);
            NotificationRuntimeStats.RecordSent(stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogInformation("Notification dispatch complete via {Channel}.", firstSuccessChannel);
        }
        else
        {
            if (_options.FallbackToLogger && channelResults.All(r => r.Channel != "Logger"))
            {
                var loggerChannel = _channels.FirstOrDefault(c => c.Name.Equals("Logger", StringComparison.OrdinalIgnoreCase));
                if (loggerChannel != null)
                {
                    var loggerResult = await DispatchToChannelAsync(loggerChannel, notification, cancellationToken);
                    channelResults.Add(loggerResult);
                    if (loggerResult.Status == ChannelDispatchStatus.Sent)
                    {
                        success = true;
                        firstSuccessChannel = loggerResult.Channel;
                    }
                }
            }

            if (!success && _options.QueueOnFailure && !fromQueue && _queue != null)
            {
                await _queue.EnqueueAsync(notification, cancellationToken);
                enqueuedForRetry = true;
                _logger.LogWarning("Notification dispatch failed; enqueued for retry.");
            }

            NotificationMetrics.NotificationsFailed.Add(1);
            NotificationRuntimeStats.RecordFailed();
            _logger.LogError("Notification dispatch failed: {Reason}", failureReason);
        }

        // Determine if any failure was due to service unavailable (e.g., missing credentials)
        serviceUnavailable = channelResults.Any(r =>
            r.Status == ChannelDispatchStatus.Failed &&
            r.Exception is TwilioSmsException smsEx &&
            smsEx.HttpStatusCode == 503);

        var result = NotificationResult.FromChannels(channelResults, failureReason, enqueuedForRetry, serviceUnavailable);

        if (!success && _options.ThrowOnFailure)
        {
            throw new NotificationFailedException(result.FailureReason ?? "Notification failed", result);
        }

        await RecordDeliveryAsync(notification, result, cancellationToken);

        return result;
    }

    public async Task<IReadOnlyCollection<NotificationResult>> SendBulkAsync(IEnumerable<INotification> notifications, bool enqueueOnly = false, CancellationToken cancellationToken = default)
    {
        var results = new List<NotificationResult>();

        foreach (var notification in notifications)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (enqueueOnly && _queue != null)
            {
                await _queue.EnqueueAsync(notification, cancellationToken);
                results.Add(NotificationResult.FromChannels(
                    new[] { ChannelDispatchResult.Skipped("Queue", "Enqueued for background delivery.") },
                    failureReason: null,
                    enqueuedForRetry: true));
                continue;
            }

            var result = await SendAsync(notification, cancellationToken);
            results.Add(result);
        }

        return results;
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

    private async Task RecordDeliveryAsync(INotification notification, NotificationResult result, CancellationToken cancellationToken)
    {
        if (_deliveryStore == null && _webhookDispatcher == null)
        {
            return;
        }

        var record = new NotificationDeliveryRecord
        {
            Type = notification.Type,
            Recipient = notification.Recipient,
            Channels = result.Channels,
            Success = result.Success,
            FailureReason = result.FailureReason
        };

        if (_deliveryStore != null)
        {
            await _deliveryStore.RecordAsync(record, cancellationToken);
        }

        if (_webhookDispatcher != null)
        {
            await _webhookDispatcher.DispatchAsync(record, cancellationToken);
        }
    }
}

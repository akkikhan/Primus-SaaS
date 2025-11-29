using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Diagnostics;

namespace PrimusSaaS.Notifications.Core;

/// <summary>
/// Bounded in-memory queue for notifications. Suitable for small to medium workloads.
/// </summary>
public class InMemoryNotificationQueue : INotificationQueue
{
    private readonly Channel<INotification> _channel;
    private readonly ILogger<InMemoryNotificationQueue> _logger;

    public ChannelReader<INotification> Reader => _channel.Reader;

    public InMemoryNotificationQueue(IOptions<NotificationQueueOptions> options, ILogger<InMemoryNotificationQueue> logger)
    {
        var opts = options.Value ?? new NotificationQueueOptions();
        _logger = logger;

        _channel = Channel.CreateBounded<INotification>(new BoundedChannelOptions(opts.BoundedCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async Task EnqueueAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));

        await _channel.Writer.WriteAsync(notification, cancellationToken);
        NotificationRuntimeStats.RecordQueued();
        NotificationMetrics.QueueLength.Add(1);
        _logger.LogDebug("Queued notification {Type} for {Recipient}", notification.Type, notification.Recipient.Email ?? notification.Recipient.UserId);
    }

    public bool TryDequeue(out INotification? notification)
    {
        if (_channel.Reader.TryRead(out notification))
        {
            NotificationMetrics.QueueLength.Add(-1);
            return true;
        }

        return false;
    }
}

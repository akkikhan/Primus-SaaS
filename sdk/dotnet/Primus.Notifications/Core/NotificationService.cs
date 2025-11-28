using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Primus.Notifications.Abstractions;

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

    public async Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting notification dispatch for {Type} to {Recipient}", notification.Type, notification.Recipient.Email ?? notification.Recipient.UserId);

        var tasks = new List<Task>();

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
            // We do not throw here to allow other channels to succeed
        }
    }
}

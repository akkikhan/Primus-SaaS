using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Primus.Notifications.Abstractions;
using Primus.Notifications.Configuration;
using Primus.Notifications.Core;
using Xunit;

namespace Primus.Notifications.Tests;

public class NotificationBackgroundServiceTests
{
    [Fact]
    public async Task BackgroundService_DrainsQueueAndSendsNotification()
    {
        var options = Options.Create(new NotificationQueueOptions
        {
            BoundedCapacity = 10,
            MaxParallelHandlers = 1,
            MaxRetryCount = 1,
            BaseRetryDelayMs = 10
        });

        var queue = new InMemoryNotificationQueue(options, NullLogger<InMemoryNotificationQueue>.Instance);
        var channel = new RecordingChannel("Email");
        var notificationService = new NotificationService(
            new IChannel[] { channel },
            NullLogger<NotificationService>.Instance);
        var scopeFactory = new SingleServiceScopeFactory(notificationService);

        var backgroundService = new NotificationBackgroundService(
            queue,
            scopeFactory,
            options,
            NullLogger<NotificationBackgroundService>.Instance);

        await backgroundService.StartAsync(CancellationToken.None);

        var notification = new TestNotification(
            "Welcome",
            new { Name = "Ada" },
            new Recipient { Email = "ada@example.com" },
            "Email");

        await queue.EnqueueAsync(notification, CancellationToken.None);

        await channel.SendTask.WaitAsync(TimeSpan.FromSeconds(2));

        await backgroundService.StopAsync(CancellationToken.None);

        Assert.Equal(1, channel.SendCount);
    }
}

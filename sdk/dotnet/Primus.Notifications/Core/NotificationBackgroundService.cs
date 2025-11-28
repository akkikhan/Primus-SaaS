using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primus.Notifications.Abstractions;
using Primus.Notifications.Configuration;

namespace Primus.Notifications.Core;

/// <summary>
/// Background worker that drains the notification queue and dispatches via NotificationService.
/// </summary>
public class NotificationBackgroundService : BackgroundService
{
    private readonly InMemoryNotificationQueue _queue;
    private readonly NotificationService _notificationService;
    private readonly NotificationQueueOptions _options;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        InMemoryNotificationQueue queue,
        NotificationService notificationService,
        IOptions<NotificationQueueOptions> options,
        ILogger<NotificationBackgroundService> logger)
    {
        _queue = queue;
        _notificationService = notificationService;
        _options = options.Value ?? new NotificationQueueOptions();
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = Enumerable.Range(0, Math.Max(1, _options.MaxParallelHandlers))
            .Select(_ => RunWorkerAsync(stoppingToken));

        return Task.WhenAll(workers);
    }

    private async Task RunWorkerAsync(CancellationToken stoppingToken)
    {
        var reader = _queue.Reader;

        while (await reader.WaitToReadAsync(stoppingToken))
        {
            while (_queue.TryDequeue(out var notification))
            {
                if (notification != null)
                {
                    await ProcessAsync(notification, stoppingToken);
                }
            }
        }
    }

    private async Task ProcessAsync(INotification notification, CancellationToken ct)
    {
        var attempt = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _notificationService.SendAsync(notification, ct);
                return;
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt > _options.MaxRetryCount)
                {
                    _logger.LogError(ex, "Notification delivery failed after {Attempts} attempts for {Type}", attempt, notification.Type);
                    return;
                }

                var delayMs = _options.BaseRetryDelayMs * Math.Pow(2, attempt - 1);
                _logger.LogWarning(ex, "Retrying notification {Type} (attempt {Attempt}/{Max}) after {Delay}ms", notification.Type, attempt + 1, _options.MaxRetryCount + 1, delayMs);
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(delayMs), ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}

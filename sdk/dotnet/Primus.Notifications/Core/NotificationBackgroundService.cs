using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;

namespace PrimusSaaS.Notifications.Core;

/// <summary>
/// Background worker that drains the notification queue and dispatches via NotificationService.
/// </summary>
public class NotificationBackgroundService : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationQueueOptions _options;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        INotificationQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationQueueOptions> options,
        ILogger<NotificationBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
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
        while (!stoppingToken.IsCancellationRequested)
        {
            INotification? notification = null;
            try
            {
                notification = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (notification == null)
            {
                await Task.Delay(100, stoppingToken);
                continue;
            }

            await ProcessAsync(notification, stoppingToken);
        }
    }

    private async Task ProcessAsync(INotification notification, CancellationToken ct)
    {
        var attempt = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Create a scope for each notification processing to get scoped services
                using var scope = _scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                var result = await notificationService.SendAsync(notification, ct, fromQueue: true);
                if (!result.Success)
                {
                    throw new NotificationFailedException(result.FailureReason ?? "Notification failed", result);
                }
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

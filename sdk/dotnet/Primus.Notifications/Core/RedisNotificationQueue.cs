using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;
using StackExchange.Redis;

namespace PrimusSaaS.Notifications.Core;

public sealed class RedisNotificationQueue : INotificationQueue
{
    private readonly IDatabase _database;
    private readonly RedisQueueOptions _options;
    private readonly ILogger<RedisNotificationQueue> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public RedisNotificationQueue(IConnectionMultiplexer multiplexer, IOptions<RedisQueueOptions> options, ILogger<RedisNotificationQueue> logger)
    {
        _database = multiplexer.GetDatabase();
        _options = options.Value ?? new RedisQueueOptions();
        _logger = logger;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task EnqueueAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        var envelope = QueuedNotificationEnvelope.From(notification, _serializerOptions);
        var payload = JsonSerializer.Serialize(envelope, _serializerOptions);
        await _database.ListLeftPushAsync(_options.QueueKey, payload);
    }

    public async ValueTask<INotification?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var value = await _database.ListRightPopAsync(_options.QueueKey);
                if (value.IsNullOrEmpty)
                {
                    await Task.Delay(_options.PollIntervalMs, cancellationToken);
                    continue;
                }

                var envelope = JsonSerializer.Deserialize<QueuedNotificationEnvelope>(value!, _serializerOptions);
                return envelope?.ToNotification();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis dequeue failed");
                await Task.Delay(_options.PollIntervalMs, cancellationToken);
            }
        }

        return null;
    }
}

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;

namespace PrimusSaaS.Notifications.Core;

public sealed class RedisDeliveryStore : INotificationDeliveryStore, IDisposable
{
    private readonly RedisDeliveryStoreOptions _options;
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _serializerOptions;
    private bool _disposed;

    public RedisDeliveryStore(IOptions<RedisDeliveryStoreOptions> options)
    {
        _options = options?.Value ?? new RedisDeliveryStoreOptions();
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("RedisDeliveryStoreOptions.ConnectionString must be configured.");
        }

        _multiplexer = ConnectionMultiplexer.Connect(_options.ConnectionString);
        _database = _multiplexer.GetDatabase();
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task RecordAsync(NotificationDeliveryRecord record, CancellationToken cancellationToken = default)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        var payload = JsonSerializer.Serialize(record, _serializerOptions);
        await _database.ListLeftPushAsync(_options.ListKey, payload);

        if (_options.MaxRecords > 0)
        {
            await _database.ListTrimAsync(_options.ListKey, 0, _options.MaxRecords - 1);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _multiplexer.Dispose();
        _disposed = true;
    }
}

using System.Collections.Concurrent;
using PrimusSaaS.Notifications.Abstractions;

namespace PrimusSaaS.Notifications.Core;

public sealed class InMemoryDeliveryStore : INotificationDeliveryStore
{
    private readonly ConcurrentQueue<NotificationDeliveryRecord> _records = new();

    public Task RecordAsync(NotificationDeliveryRecord record, CancellationToken cancellationToken = default)
    {
        _records.Enqueue(record);
        while (_records.Count > 1000 && _records.TryDequeue(out _))
        {
            // keep bounded history
        }
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<NotificationDeliveryRecord> Records => _records.ToArray();
}

using System.Threading;
using System.Threading.Tasks;
using PrimusSaaS.Notifications.Core;

namespace PrimusSaaS.Notifications.Abstractions;

public interface INotificationDeliveryStore
{
    Task RecordAsync(NotificationDeliveryRecord record, CancellationToken cancellationToken = default);
}

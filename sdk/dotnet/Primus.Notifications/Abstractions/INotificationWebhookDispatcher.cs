using System.Threading;
using System.Threading.Tasks;
using PrimusSaaS.Notifications.Core;

namespace PrimusSaaS.Notifications.Abstractions;

public interface INotificationWebhookDispatcher
{
    Task DispatchAsync(NotificationDeliveryRecord record, CancellationToken cancellationToken = default);
}

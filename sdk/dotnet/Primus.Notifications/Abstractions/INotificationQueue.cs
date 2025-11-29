using System.Threading;
using System.Threading.Tasks;

namespace PrimusSaaS.Notifications.Abstractions;

/// <summary>
/// Contract for queueing notifications to be processed out-of-band.
/// </summary>
public interface INotificationQueue
{
    Task EnqueueAsync(INotification notification, CancellationToken cancellationToken = default);
}

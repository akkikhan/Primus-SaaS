using System.Threading;
using System.Threading.Tasks;

namespace PrimusSaaS.Notifications.Abstractions;

/// <summary>
/// Contract for queueing notifications to be processed out-of-band.
/// </summary>
public interface INotificationQueue
{
    Task EnqueueAsync(INotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues the next notification, waiting until one is available or cancellation is requested.
    /// Returns null when cancelled or when the queue is drained and non-blocking implementations choose to yield.
    /// </summary>
    ValueTask<INotification?> DequeueAsync(CancellationToken cancellationToken = default);
}

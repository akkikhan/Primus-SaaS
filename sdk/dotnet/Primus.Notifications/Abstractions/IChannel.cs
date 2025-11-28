using System.Threading;
using System.Threading.Tasks;

namespace Primus.Notifications.Abstractions;

public interface IChannel
{
    /// <summary>
    /// The name of the channel (e.g., "Email", "SMS", "InApp").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sends the notification via this channel.
    /// </summary>
    Task SendAsync(INotification notification, CancellationToken cancellationToken = default);
}

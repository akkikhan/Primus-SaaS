using System.Threading;
using System.Threading.Tasks;

namespace PrimusSaaS.Notifications.Abstractions;

/// <summary>
/// Abstraction for sending SMS messages. Implement this to integrate with your provider (e.g., Twilio).
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string to, string message, CancellationToken cancellationToken = default);
}

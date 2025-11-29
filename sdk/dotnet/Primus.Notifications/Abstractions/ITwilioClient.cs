using System.Threading;
using System.Threading.Tasks;

namespace PrimusSaaS.Notifications.Abstractions;

/// <summary>
/// Abstraction for Twilio SMS operations to aid DI and testing.
/// </summary>
public interface ITwilioClient
{
    Task SendSmsAsync(string to, string body, CancellationToken cancellationToken = default);
}

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Primus.Notifications.Abstractions;

namespace Primus.Notifications.Channels.Sms;

/// <summary>
/// Default SMS sender that logs messages instead of delivering to a provider.
/// Useful for local/dev environments and as a safe fallback.
/// </summary>
public class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SMS to {Phone}: {Message}", to, message);
        return Task.CompletedTask;
    }
}

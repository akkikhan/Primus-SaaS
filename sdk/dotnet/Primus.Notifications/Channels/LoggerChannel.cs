using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Primus.Notifications.Abstractions;

namespace Primus.Notifications.Channels;

public class LoggerChannel : IChannel
{
    public string Name => "Logger";
    private readonly ILogger<LoggerChannel> _logger;

    public LoggerChannel(ILogger<LoggerChannel> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📢 [NOTIFICATION] Type: {Type} | Recipient: {Recipient} | Data: {Data}", 
            notification.Type, 
            notification.Recipient.Email ?? notification.Recipient.UserId, 
            notification.Data);
        return Task.CompletedTask;
    }
}

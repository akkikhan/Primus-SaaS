using System;
using System.Threading;
using System.Threading.Tasks;
using PrimusSaaS.Notifications.Abstractions;

namespace PrimusNotificationTest;

public class ConsoleChannel : IChannel
{
    public string Name => "Console";

    public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[Console Channel] Sending {notification.Type} to {notification.Recipient.Name}");
        Console.ResetColor();
        return Task.CompletedTask;
    }
}

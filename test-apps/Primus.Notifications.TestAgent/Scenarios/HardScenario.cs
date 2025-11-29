using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;
using Spectre.Console;

namespace PrimusSaaS.Notifications.TestAgent.Scenarios;

public class HardScenario : IScenario
{
    public string Name => "Multi-Channel Partial Failure";
    public string Description => "Validates resilience. Sends to Email (Success) and SMS (Simulated Failure). Ensures the process doesn't crash.";
    public string Difficulty => "Hard";

    public async Task RunAsync(IServiceProvider services)
    {
        var notifier = services.GetRequiredService<NotificationService>();
        
        var notification = new MultiChannelNotification(
            "SecurityAlert",
            new { IP = "192.168.1.1" },
            new Recipient { Email = "admin@primus.com", PhoneNumber = "+15550000" }
        );

        try 
        {
            await notifier.SendAsync(notification);
            AnsiConsole.MarkupLine("[green]✓ Hard Scenario Complete (Graceful degradation handled)[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Hard Scenario Failed: {ex.Message}[/]");
        }
    }
}

public class MultiChannelNotification : INotification
{
    public string Type { get; }
    public object Data { get; }
    public IEnumerable<string> Channels => new[] { "Email", "SMS" }; // SMS will fail
    public Recipient Recipient { get; }

    public MultiChannelNotification(string type, object data, Recipient recipient)
    {
        Type = type;
        Data = data;
        Recipient = recipient;
    }
}

// Mock SMS Channel that fails
public class FailingSmsChannel : IChannel
{
    public string Name => "SMS";
    public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        throw new Exception("Simulated SMS Gateway Timeout");
    }
}

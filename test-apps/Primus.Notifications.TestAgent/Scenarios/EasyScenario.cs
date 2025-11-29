using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;
using Spectre.Console;

namespace PrimusSaaS.Notifications.TestAgent.Scenarios;

public class EasyScenario : IScenario
{
    public string Name => "Welcome Email";
    public string Description => "Validates basic single-channel dispatch with simple variable substitution.";
    public string Difficulty => "Easy";

    public async Task RunAsync(IServiceProvider services)
    {
        var notifier = services.GetRequiredService<NotificationService>();
        
        var notification = new SimpleNotification(
            "Welcome", 
            new { Name = "John Doe" }, 
            new Recipient { Email = "john@example.com", Name = "John Doe" }
        );

        await notifier.SendAsync(notification);
        AnsiConsole.MarkupLine("[green]✓ Easy Scenario Complete[/]");
    }
}

public class SimpleNotification : INotification
{
    public string Type { get; }
    public object Data { get; }
    public IEnumerable<string> Channels => new[] { "Email" };
    public Recipient Recipient { get; }

    public SimpleNotification(string type, object data, Recipient recipient)
    {
        Type = type;
        Data = data;
        Recipient = recipient;
    }
}

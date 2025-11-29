using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;
using Spectre.Console;

namespace PrimusSaaS.Notifications.TestAgent.Scenarios;

public class ModerateScenario : IScenario
{
    public string Name => "Invoice Generated";
    public string Description => "Validates complex Liquid templates with loops (for line items) and conditional logic.";
    public string Difficulty => "Moderate";

    public async Task RunAsync(IServiceProvider services)
    {
        var notifier = services.GetRequiredService<NotificationService>();
        
        var data = new
        {
            InvoiceId = "INV-2024-001",
            Total = 199.99,
            IsPaid = false,
            Items = new[]
            {
                new { Description = "Primus SaaS Pro License", Price = 99.99 },
                new { Description = "Advanced Security Module", Price = 49.00 },
                new { Description = "Priority Support", Price = 51.00 }
            }
        };

        var notification = new SimpleNotification(
            "Invoice", 
            data, 
            new Recipient { Email = "billing@corp.com", Name = "Billing Dept" }
        );

        await notifier.SendAsync(notification);
        AnsiConsole.MarkupLine("[green]✓ Moderate Scenario Complete[/]");
    }
}

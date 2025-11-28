using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Primus.Notifications.Abstractions;
using Primus.Notifications.Core;
using Spectre.Console;

namespace Primus.Notifications.TestAgent.Scenarios;

public class ComplexScenario : IScenario
{
    public string Name => "High Concurrency Load Test";
    public string Description => "Simulates 100 parallel notifications to verify thread safety and template caching performance.";
    public string Difficulty => "Complex";

    public async Task RunAsync(IServiceProvider services)
    {
        var notifier = services.GetRequiredService<NotificationService>();
        
        var tasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();

        AnsiConsole.MarkupLine("[yellow]Starting 100 parallel dispatches...[/]");

        for (int i = 0; i < 100; i++)
        {
            var id = i;
            tasks.Add(Task.Run(async () => 
            {
                var notification = new SimpleNotification(
                    "Welcome", 
                    new { Name = $"User {id}" }, 
                    new Recipient { Email = $"user{id}@example.com" }
                );
                await notifier.SendAsync(notification);
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        AnsiConsole.MarkupLine($"[green]✓ Complex Scenario Complete[/]");
        AnsiConsole.MarkupLine($"[blue]  Time taken: {stopwatch.ElapsedMilliseconds}ms[/]");
        AnsiConsole.MarkupLine($"[blue]  Avg per notification: {stopwatch.ElapsedMilliseconds / 100.0}ms[/]");
    }
}

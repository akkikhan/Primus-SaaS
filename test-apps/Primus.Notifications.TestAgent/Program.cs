using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;
using PrimusSaaS.Notifications.TestAgent.Scenarios;
using Spectre.Console;

namespace PrimusSaaS.Notifications.TestAgent;

class Program
{
    static async Task Main(string[] args)
    {
        // Display banner
        var rule = new Rule("[bold blue]Primus Notification Module - Test Agent[/]")
            .Centered();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Setup DI
        var services = new ServiceCollection();
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        // Define scenarios
        var scenarios = new IScenario[]
        {
            new EasyScenario(),
            new ModerateScenario(),
            new HardScenario(),
            new ComplexScenario()
        };

        // Display scenario menu
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[yellow]#[/]");
        table.AddColumn("[yellow]Difficulty[/]");
        table.AddColumn("[yellow]Scenario[/]");
        table.AddColumn("[yellow]Description[/]");

        for (int i = 0; i < scenarios.Length; i++)
        {
            var s = scenarios[i];
            var difficultyColor = s.Difficulty switch
            {
                "Easy" => "green",
                "Moderate" => "yellow",
                "Hard" => "orange1",
                "Complex" => "red",
                _ => "white"
            };
            
            table.AddRow(
                $"{i + 1}",
                $"[{difficultyColor}]{s.Difficulty}[/]",
                $"[bold]{s.Name}[/]",
                s.Description
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Run scenarios
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select test mode:")
                .AddChoices(new[] { "Run All", "Run Single", "Exit" })
        );

        if (choice == "Exit") return;

        if (choice == "Run All")
        {
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Running all scenarios[/]", maxValue: scenarios.Length);
                    
                    foreach (var scenario in scenarios)
                    {
                        AnsiConsole.MarkupLine($"\n[bold cyan]→ Running: {scenario.Name}[/]");
                        await scenario.RunAsync(provider);
                        task.Increment(1);
                    }
                });
        }
        else if (choice == "Run Single")
        {
            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<IScenario>()
                    .Title("Select scenario:")
                    .AddChoices(scenarios)
                    .UseConverter(s => $"{s.Difficulty} - {s.Name}")
            );

            AnsiConsole.MarkupLine($"\n[bold cyan]→ Running: {selected.Name}[/]");
            await selected.RunAsync(provider);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]✓ Test Agent Complete[/]");
    }

    static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(configure => 
        {
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Information);
        });

        services.AddPrimusNotifications(config =>
        {
            config.UseSmtp(options =>
            {
                options.Host = "localhost";
                options.Port = 25;
                options.FromAddress = "test@primus.com";
                options.FromName = "Primus Test Agent";
            });

            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            if (!Directory.Exists(templatePath))
            {
                templatePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Templates"));
            }
            
            config.UseFileTemplates(templatePath);
            config.UseLogger();
        });

        // Register the failing SMS channel for Hard scenario
        services.AddScoped<IChannel, FailingSmsChannel>();
    }
}

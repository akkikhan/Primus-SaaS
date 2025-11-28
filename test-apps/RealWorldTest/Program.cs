using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Primus.Notifications;
using Primus.Notifications.Abstractions;
using Primus.Notifications.Core;

namespace RealWorldTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  PRIMUS NOTIFICATION MODULE - REAL WORLD TEST");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        // Setup services
        var services = new ServiceCollection();
        services.AddLogging(config => config.AddConsole().SetMinimumLevel(LogLevel.Information));

        services.AddPrimusNotifications(config =>
        {
            config.UseSmtp(options =>
            {
                options.Host = "smtp.gmail.com";
                options.Port = 587;
                options.FromAddress = "noreply@primussaas.com";
                options.FromName = "Primus SaaS";
                options.Username = "test@example.com";
                options.Password = "test-password";
            });

            var templatePath = Path.GetFullPath("../../../Templates");
            config.UseFileTemplates(templatePath);
            config.UseLogger();
        });

        var provider = services.BuildServiceProvider();
        var notifier = provider.GetRequiredService<NotificationService>();

        // Real-World Scenario: New Application Created
        Console.WriteLine("📋 SCENARIO: New Application Registration");
        Console.WriteLine("─────────────────────────────────────────────────────────\n");

        var appCreatedNotification = new SimpleNotification(
            "ApplicationCreated",
            new
            {
                AppName = "E-Commerce Platform",
                ClientId = "primus_abc123xyz789"
            },
            new Recipient
            {
                Email = "developer@company.com",
                Name = "John Developer"
            }
        );

        Console.WriteLine("Dispatching ApplicationCreated notification...");
        await notifier.SendAsync(appCreatedNotification);
        Console.WriteLine("✓ Notification dispatched\n");

        // Real-World Scenario: Module Assigned
        Console.WriteLine("\n📋 SCENARIO: Identity Module Assigned to Application");
        Console.WriteLine("─────────────────────────────────────────────────────────\n");

        var moduleAssignedNotification = new SimpleNotification(
            "ModuleAssigned",
            new
            {
                ModuleName = "Identity Validator",
                Version = "1.2.0",
                AppName = "E-Commerce Platform",
                InstallCommand = "npm install @primus-saas/identity-validator",
                DocLink = "https://akkikhan.github.io/Primus-SaaS/docs/modules/identity",
                Stack = "nodejs",
                DocsBaseUrl = "https://akkikhan.github.io/Primus-SaaS"
            },
            new Recipient
            {
                Email = "developer@company.com",
                Name = "John Developer"
            }
        );

        Console.WriteLine("Dispatching ModuleAssigned notification...");
        await notifier.SendAsync(moduleAssignedNotification);
        Console.WriteLine("✓ Notification dispatched\n");

        // Real-World Scenario: New Version Published
        Console.WriteLine("\n📋 SCENARIO: New Module Version Published");
        Console.WriteLine("─────────────────────────────────────────────────────────\n");

        var versionPublishedNotification = new SimpleNotification(
            "VersionPublished",
            new
            {
                Version = "1.3.0",
                ModuleName = "Identity Validator",
                NpmPackageName = "@primus-saas/identity-validator",
                NugetPackageName = "PrimusSaaS.Identity.Validator",
                ReleaseNotes = "Added support for Azure AD B2C",
                Changelog = "- New: Azure AD B2C integration\n- Fix: Token validation edge case\n- Perf: 20% faster validation",
                IsBreakingChange = false
            },
            new Recipient
            {
                Email = "developer@company.com",
                Name = "John Developer"
            }
        );

        Console.WriteLine("Dispatching VersionPublished notification...");
        await notifier.SendAsync(versionPublishedNotification);
        Console.WriteLine("✓ Notification dispatched\n");

        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("  ✓ REAL WORLD TEST COMPLETE");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("\nCheck the console output above for Logger channel output.");
        Console.WriteLine("In production, these would be actual emails sent via SMTP.\n");
    }
}

public class SimpleNotification : INotification
{
    public string Type { get; }
    public object Data { get; }
    public IEnumerable<string> Channels => new[] { "Logger", "Email" };
    public Recipient Recipient { get; }

    public SimpleNotification(string type, object data, Recipient recipient)
    {
        Type = type;
        Data = data;
        Recipient = recipient;
    }
}

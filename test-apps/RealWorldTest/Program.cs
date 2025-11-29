using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;

namespace RealWorldTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("==========================================================");
        Console.WriteLine("  PRIMUS NOTIFICATION MODULE - REAL WORLD QUEUE TEST");
        Console.WriteLine("==========================================================\n");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                services.AddPrimusNotifications(config =>
                {
                    var templatePath = Path.GetFullPath("Templates");
                    config.UseFileTemplates(templatePath);
                    config.UseLogger();
                    config.UseInMemoryQueue(options =>
                    {
                        options.BoundedCapacity = 500;
                        options.MaxParallelHandlers = 2;
                        options.MaxRetryCount = 1;
                        options.BaseRetryDelayMs = 200;
                    });

                    var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
                    if (!string.IsNullOrWhiteSpace(smtpHost))
                    {
                        config.UseSmtp(options =>
                        {
                            options.Host = smtpHost;
                            options.Port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
                            options.FromAddress = Environment.GetEnvironmentVariable("SMTP_FROM") ?? "noreply@primussaas.com";
                            options.FromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "Primus SaaS";
                            options.Username = Environment.GetEnvironmentVariable("SMTP_USER") ?? string.Empty;
                            options.Password = Environment.GetEnvironmentVariable("SMTP_PASS") ?? string.Empty;
                            options.EnableSsl = (Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL") ?? "true").Equals("true", StringComparison.OrdinalIgnoreCase);
                        });
                    }
                });
            })
            .Build();

        await host.StartAsync();
        var queue = host.Services.GetRequiredService<INotificationQueue>();

        Console.WriteLine("🗂️ SCENARIO: New Application Registration\n--------------------------------------------------");
        await queue.EnqueueAsync(new SimpleNotification(
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
        ));
        await Task.Delay(300);
        Console.WriteLine("✓ Notification dispatched (queued)\n");

        Console.WriteLine("🗂️ SCENARIO: Identity Module Assigned to Application\n--------------------------------------------------");
        await queue.EnqueueAsync(new SimpleNotification(
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
        ));
        await Task.Delay(300);
        Console.WriteLine("✓ Notification dispatched (queued)\n");

        Console.WriteLine("🗂️ SCENARIO: New Module Version Published\n--------------------------------------------------");
        await queue.EnqueueAsync(new SimpleNotification(
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
        ));
        await Task.Delay(300);
        Console.WriteLine("✓ Notification dispatched (queued)\n");

        Console.WriteLine("\n==========================================================");
        Console.WriteLine("  ✓ REAL WORLD QUEUE TEST COMPLETE");
        Console.WriteLine("==========================================================");
        Console.WriteLine("\nCheck console output for Logger channel dispatch. SMTP is used only if SMTP_* env vars are provided.\n");

        await host.StopAsync();
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

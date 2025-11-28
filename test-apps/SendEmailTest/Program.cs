using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Primus.Notifications;
using Primus.Notifications.Abstractions;
using Primus.Notifications.Core;

namespace SendEmailTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  PRIMUS NOTIFICATION MODULE - SEND EMAIL TEST");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        // SMTP Configuration
        Console.WriteLine("📧 SMTP Configuration Required\n");
        Console.WriteLine("To send a real email, please provide SMTP credentials:");
        Console.WriteLine("(Press Enter to use demo mode with Logger channel only)\n");

        Console.Write("SMTP Host (e.g., smtp.gmail.com): ");
        var smtpHost = Console.ReadLine();

        bool useDemoMode = string.IsNullOrWhiteSpace(smtpHost);

        // Setup services
        var services = new ServiceCollection();
        services.AddLogging(config => config.AddConsole().SetMinimumLevel(LogLevel.Information));

        services.AddPrimusNotifications(config =>
        {
            if (!useDemoMode)
            {
                Console.Write("SMTP Port (default 587): ");
                var portInput = Console.ReadLine();
                int port = string.IsNullOrWhiteSpace(portInput) ? 587 : int.Parse(portInput);

                Console.Write("SMTP Username: ");
                var username = Console.ReadLine();

                Console.Write("SMTP Password: ");
                var password = ReadPassword();
                Console.WriteLine();

                Console.Write("From Email Address: ");
                var fromAddress = Console.ReadLine();

                config.UseSmtp(options =>
                {
                    options.Host = smtpHost;
                    options.Port = port;
                    options.Username = username;
                    options.Password = password;
                    options.FromAddress = fromAddress ?? "noreply@primussaas.com";
                    options.FromName = "Primus SaaS";
                });
            }

            var templatePath = Path.GetFullPath("../../../Templates");
            if (!Directory.Exists(templatePath))
            {
                // Try alternate path
                templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates");
            }
            
            config.UseFileTemplates(templatePath);
            config.UseLogger();
        });

        var provider = services.BuildServiceProvider();
        var notifier = provider.GetRequiredService<NotificationService>();

        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("  SENDING TEST EMAIL TO akki@primussoft.com");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        // Create notification
        var notification = new WelcomeNotification(
            "Akki",
            "akki@primussoft.com"
        );

        try
        {
            Console.WriteLine("📤 Dispatching notification...\n");
            await notifier.SendAsync(notification);
            
            Console.WriteLine("\n✅ SUCCESS!");
            
            if (useDemoMode)
            {
                Console.WriteLine("\n⚠️  DEMO MODE: Email was not actually sent (no SMTP configured)");
                Console.WriteLine("   Check the Logger output above to see the notification data.");
                Console.WriteLine("\n   To send a real email, run the program again and provide SMTP credentials.");
            }
            else
            {
                Console.WriteLine("\n📧 Email sent successfully to akki@primussoft.com");
                Console.WriteLine("   Check your inbox!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ERROR: {ex.Message}");
            Console.WriteLine($"\nDetails: {ex}");
        }

        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static string ReadPassword()
    {
        string password = "";
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);

            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                password += key.KeyChar;
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, password.Length - 1);
                Console.Write("\b \b");
            }
        }
        while (key.Key != ConsoleKey.Enter);

        return password;
    }
}

public class WelcomeNotification : INotification
{
    public string Type => "Welcome";
    public object Data { get; }
    public IEnumerable<string> Channels { get; }
    public Recipient Recipient { get; }

    public WelcomeNotification(string name, string email, bool useSmtp = true)
    {
        Data = new { Name = name };
        Channels = useSmtp ? new[] { "Email", "Logger" } : new[] { "Logger" };
        Recipient = new Recipient
        {
            Name = name,
            Email = email
        };
    }
}

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;

namespace PrimusNotificationTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("--- Primus Notification Module Test ---");

        // 1. Setup Dependency Injection
        var services = new ServiceCollection();
        
        services.AddLogging(configure => configure.AddConsole());

        // 2. Configure Primus Notifications
        services.AddPrimusNotifications(config =>
        {
            // Configure Email (Simulated)
            config.UseSmtp(options =>
            {
                options.Host = "localhost";
                options.Port = 25;
                options.FromAddress = "test@primus.com";
            });

            // Configure Templates
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            // For this test, we assume templates are copied to output or we point to source
            // Let's point to source for simplicity in this dev environment
            templatePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Templates"));
            config.UseFileTemplates(templatePath);
        });

        // 3. Register Custom Channel (Demonstrating Extensibility)
        services.AddScoped<IChannel, ConsoleChannel>();

        var provider = services.BuildServiceProvider();

        // 4. Get the Dispatcher
        var notifier = provider.GetRequiredService<NotificationService>();

        // 5. Create a Notification
        var notification = new WelcomeNotification("Akki", "akki@example.com");

        Console.WriteLine("Dispatching notification...");
        
        // 6. Send!
        try 
        {
            await notifier.SendAsync(notification);
            Console.WriteLine("--- Dispatch Complete ---");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

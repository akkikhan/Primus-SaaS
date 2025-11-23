using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Examples;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== .NET Logging SDK Demo ===\n");

        // Create logger
        var logger = new Logger(new LoggerOptions
        {
            ApplicationId = "DOTNET-DEMO-123",
            Environment = "development",
            MinLevel = LogLevel.Debug,
            Targets = new List<TargetConfig>
            {
                new() { Type = "console", Pretty = true }
            }
        });

        // Basic logging
        Console.WriteLine("=== Basic Logging ===\n");
        logger.Debug("This is a debug message");
        logger.Info("Application started");
        logger.Warn("This is a warning");
        logger.Error("This is an error");
        logger.Critical("This is critical!");

        // Logging with context
        Console.WriteLine("\n=== Logging with Context ===\n");
        logger.Info("User logged in", new Dictionary<string, object>
        {
            ["userId"] = "12345",
            ["username"] = "john.doe",
            ["ipAddress"] = "192.168.1.1"
        });

        logger.Info("Order created", new Dictionary<string, object>
        {
            ["orderId"] = "ORD-789",
            ["amount"] = 99.99,
            ["currency"] = "USD"
        });

        // Performance tracking
        Console.WriteLine("\n=== Performance Tracking ===\n");
        var timer = logger.StartTimer();
        Thread.Sleep(100);
        timer.Done("Order processed", new Dictionary<string, object>
        {
            ["orderId"] = "ORD-789"
        });

        // Correlation IDs
        Console.WriteLine("\n=== Correlation IDs ===\n");
        var correlationId = logger.GenerateCorrelationId();
        logger.Info("Checkout initiated", new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["cartTotal"] = 199.99
        });
        logger.Info("Inventory reserved", new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["items"] = 3
        });
        logger.Info("Payment processed", new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["amount"] = 199.99
        });

        // Log level filtering
        Console.WriteLine("\n=== Log Level Filtering ===\n");
        var prodLogger = new Logger(new LoggerOptions
        {
            ApplicationId = "PROD-APP-456",
            Environment = "production",
            MinLevel = LogLevel.Info
        });

        prodLogger.Debug("This will NOT be logged");
        prodLogger.Info("This WILL be logged");

        Console.WriteLine("\n=== Demo Complete ===");
    }
}

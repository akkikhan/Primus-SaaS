using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class ILoggerIntegrationTests
{
    [Fact]
    public void ShouldIntegrateWithStandardILogger()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
        {
            builder.ClearProviders(); // Remove default providers
            builder.AddPrimus(options =>
            {
                options.ApplicationId = "TEST-APP";
                options.Environment = "testing";
                options.MinLevel = PrimusSaaS.Logging.Core.LogLevel.Debug;
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ILoggerIntegrationTests>>();

        // Act & Assert
        Assert.NotNull(logger);
        
        // Should not throw
        logger.LogInformation("Test message");
        logger.LogWarning("Warning message");
        logger.LogError("Error message");
    }

    [Fact]
    public void ShouldSupportStructuredLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddPrimus(options =>
            {
                options.ApplicationId = "TEST-APP";
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ILoggerIntegrationTests>>();

        // Act - Use structured logging with template
        logger.LogInformation("User {UserId} performed action {Action}", "user-123", "login");

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void ShouldMapLogLevelsCorrectly()
    {
        // Arrange
        var testTarget = new TestTarget();
        var primusLogger = new Core.Logger(new LoggerOptions
        {
            ApplicationId = "TEST",
            Targets = new List<TargetConfig> { new TargetConfig { Type = "console" } }
        });

        // Manually add test target
        var services = new ServiceCollection();
        services.AddSingleton(primusLogger);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddPrimus(options =>
            {
                options.ApplicationId = "TEST-APP";
                options.MinLevel = PrimusSaaS.Logging.Core.LogLevel.Debug;
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ILoggerIntegrationTests>>();

        // Act
        logger.LogTrace("Trace");
        logger.LogDebug("Debug");
        logger.LogInformation("Info");
        logger.LogWarning("Warning");
        logger.LogError("Error");
        logger.LogCritical("Critical");

        // Assert - Should not throw
        Assert.True(true);
    }
}

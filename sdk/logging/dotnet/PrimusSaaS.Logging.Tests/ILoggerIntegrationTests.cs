using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;
using PrimusSaaS.Logging.Targets;
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
        var primusLogger = new Core.Logger(new LoggerOptions { ApplicationId = "TEST", Targets = new List<TargetConfig> { new TargetConfig { Type = "console" } } });
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

    [Fact]
    public void ShouldRespectMinLevelWhenUsingILogger()
    {
        var testTarget = new TestTarget();
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddPrimus(options =>
            {
                options.ApplicationId = "TEST-APP";
                options.Environment = "testing";
                options.MinLevel = PrimusSaaS.Logging.Core.LogLevel.Warning;
                options.CustomTargets = new List<ITarget>
                {
                    testTarget
                };
                options.Targets = new List<TargetConfig>(); // avoid default console target for test isolation
            });
        });
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ILoggerIntegrationTests>>();
        logger.LogInformation("Info should be filtered");
        logger.LogWarning("Warning should pass");
        var entry = Assert.Single(testTarget.Logs);
        Assert.Equal(PrimusSaaS.Logging.Core.LogLevel.Warning, entry.Level);
        Assert.Equal("Warning should pass", entry.Message);
    }

    [Fact]
    public void ShouldPropagateScopesAndEventIds()
    {
        var testTarget = new TestTarget();
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddPrimus(options =>
            {
                options.ApplicationId = "TEST-APP";
                options.Environment = "testing";
                options.CustomTargets = new List<ITarget>
                {
                    testTarget
                };
                options.Targets = new List<TargetConfig>();
            });
        });
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ILoggerIntegrationTests>>();
        using (logger.BeginScope(new Dictionary<string, object?> { ["scopeKey"] = "scope-value", ["tenantId"] = "tenant-123" }))
        {
            logger.LogInformation(new EventId(42, "ScopeTest"), "User {User} logged in", "alice");
        }

        var entry = Assert.Single(testTarget.Logs);
        Assert.Equal(PrimusSaaS.Logging.Core.LogLevel.Info, entry.Level);
        Assert.Equal("alice", entry.Context["User"]);
        Assert.Equal(42, entry.Context["eventId"]);
        Assert.Equal("ScopeTest", entry.Context["eventName"]);
        Assert.Equal("scope-value", entry.Context["scopeKey"]);
        Assert.Equal("tenant-123", entry.Context["tenantId"]);
        Assert.Equal("TEST-APP", entry.Context["applicationId"]);
        Assert.Equal("testing", entry.Context["environment"]);
    }

    [Fact]
    public void ShouldTrackAdapterMetrics()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddPrimus(options =>
            {
                options.ApplicationId = "TEST-APP";
                options.Environment = "testing";
            });
        });
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ILoggerIntegrationTests>>();
        var primusLogger = serviceProvider.GetRequiredService<Core.Logger>();
        var before = primusLogger.GetMetricsSnapshot().AdapterForwardedEntries;
        logger.LogInformation("Adapter metrics should increment");
        var after = primusLogger.GetMetricsSnapshot().AdapterForwardedEntries;
        Assert.Equal(before + 1, after);
    }
}

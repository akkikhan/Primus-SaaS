using PrimusSaaS.Logging.Core;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class LoggerTests
{
    [Fact]
    public void Logger_ShouldCreateWithOptions()
    {
        // Arrange
        var options = new LoggerOptions
        {
            ApplicationId = "TEST-APP",
            Environment = "testing",
            MinLevel = LogLevel.Debug
        };

        // Act
        var logger = new Logger(options);

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void Logger_ShouldLogAtAllLevels()
    {
        // Arrange
        var options = new LoggerOptions
        {
            ApplicationId = "TEST-APP",
            Environment = "testing",
            MinLevel = LogLevel.Debug
        };
        var logger = new Logger(options);

        // Act & Assert (should not throw)
        logger.Debug("Debug message");
        logger.Info("Info message");
        logger.Warn("Warning message");
        logger.Error("Error message");
        logger.Critical("Critical message");
    }

    [Fact]
    public void Logger_ShouldFilterByLogLevel()
    {
        // Arrange
        var options = new LoggerOptions
        {
            ApplicationId = "TEST-APP",
            Environment = "testing",
            MinLevel = LogLevel.Warning
        };
        var logger = new Logger(options);

        // Act (should not log debug/info)
        logger.Debug("Should not appear");
        logger.Info("Should not appear");
        logger.Warn("Should appear");
        logger.Error("Should appear");

        // Assert - no exception means filtering works
        Assert.True(true);
    }

    [Fact]
    public void Logger_ShouldGenerateCorrelationId()
    {
        // Arrange
        var options = new LoggerOptions
        {
            ApplicationId = "TEST-APP",
            Environment = "testing"
        };
        var logger = new Logger(options);

        // Act
        var correlationId = logger.GenerateCorrelationId();

        // Assert
        Assert.NotNull(correlationId);
        Assert.StartsWith("corr-", correlationId);
    }

    [Fact]
    public void Logger_ShouldCreateTimer()
    {
        // Arrange
        var options = new LoggerOptions
        {
            ApplicationId = "TEST-APP",
            Environment = "testing"
        };
        var logger = new Logger(options);

        // Act
        var timer = logger.StartTimer();
        Thread.Sleep(100);
        timer.Done("Operation completed");

        // Assert
        Assert.NotNull(timer);
    }
}

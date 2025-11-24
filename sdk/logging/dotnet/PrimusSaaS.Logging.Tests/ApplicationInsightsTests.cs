using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class ApplicationInsightsTests
{
    [Fact]
    public void ShouldInitializeWithConnectionString()
    {
        // Arrange
        var connectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://eastus-2.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/";
        
        // Act
        var target = new ApplicationInsightsTarget(connectionString);

        // Assert
        Assert.NotNull(target);
    }

    [Fact]
    public void ShouldNotThrowOnWrite()
    {
        // Arrange
        // Using a dummy key - the SDK handles this gracefully by just dropping or queuing
        var connectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"; 
        var target = new ApplicationInsightsTarget(connectionString);
        var entry = LogEntry.Create(LogLevel.Info, "Test message", new Dictionary<string, object?> { ["key"] = "value" });

        // Act & Assert
        try
        {
            target.Write(entry);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Write should not throw exception: {ex.Message}");
        }
    }
}

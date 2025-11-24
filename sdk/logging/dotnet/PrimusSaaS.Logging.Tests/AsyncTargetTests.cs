using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class AsyncTargetTests
{
    [Fact]
    public void ShouldWriteLogsAsynchronously()
    {
        // Arrange
        var innerTarget = new TestTarget();
        var asyncTarget = new AsyncTargetWrapper(innerTarget, bufferSize: 100);
        var entry = LogEntry.Create(LogLevel.Info, "Async message");

        // Act
        asyncTarget.Write(entry);
        asyncTarget.Write(entry);
        asyncTarget.Write(entry);

        // Assert - immediately after write, logs might not be there yet
        // Wait a bit
        Thread.Sleep(100);
        
        asyncTarget.Close();

        Assert.Equal(3, innerTarget.Logs.Count);
        Assert.True(innerTarget.IsClosed);
    }

    [Fact]
    public void ShouldFlushBufferOnClose()
    {
        // Arrange
        var innerTarget = new TestTarget();
        var asyncTarget = new AsyncTargetWrapper(innerTarget, bufferSize: 100);
        
        // Act
        for (int i = 0; i < 50; i++)
        {
            asyncTarget.Write(LogEntry.Create(LogLevel.Info, $"Msg {i}"));
        }

        // Close immediately without waiting
        asyncTarget.Close();

        // Assert - Close should have waited for buffer to drain
        Assert.Equal(50, innerTarget.Logs.Count);
    }
}

using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class FileRotationTests : IDisposable
{
    private readonly string _testDir;

    public FileRotationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "PrimusLoggingTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    [Fact]
    public void ShouldRotateFilesWhenSizeExceeded()
    {
        // Arrange
        var logFile = Path.Combine(_testDir, "test.log");
        var maxBytes = 100; // Very small limit
        var target = new FileTarget(logFile, maxBytes, 2, false);
        var entry = LogEntry.Create(LogLevel.Info, "12345678901234567890"); // ~100 bytes with JSON overhead

        // Act
        // Write enough to trigger rotation
        target.Write(entry); // File created
        target.Write(entry); // Should trigger rotation soon
        target.Write(entry); // Should trigger rotation
        target.Write(entry); 

        target.Close();

        // Assert
        Assert.True(File.Exists(logFile), "Main log file should exist");
        Assert.True(File.Exists(logFile + ".1"), "Rotated file .1 should exist");
        
        // Check content
        var mainContent = File.ReadAllText(logFile);
        var rotatedContent = File.ReadAllText(logFile + ".1");
        
        Assert.NotEmpty(mainContent);
        Assert.NotEmpty(rotatedContent);
    }

    [Fact]
    public void ShouldCompressRotatedFiles()
    {
        // Arrange
        var logFile = Path.Combine(_testDir, "compress.log");
        var maxBytes = 100;
        var target = new FileTarget(logFile, maxBytes, 2, true); // Enable compression
        var entry = LogEntry.Create(LogLevel.Info, "Data to compress");

        // Act
        target.Write(entry);
        target.Write(entry);
        target.Write(entry); // Trigger rotation
        
        target.Close();

        // Assert
        Assert.True(File.Exists(logFile), "Main log file should exist");
        Assert.False(File.Exists(logFile + ".1"), "Uncompressed .1 should NOT exist");
        Assert.True(File.Exists(logFile + ".1.gz"), "Compressed .1.gz should exist");
    }
}

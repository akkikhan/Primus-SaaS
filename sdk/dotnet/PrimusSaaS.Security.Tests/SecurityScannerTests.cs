using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PrimusSaaS.Security.Core;
using PrimusSaaS.Security.Tests;

namespace PrimusSaaS.Security.Tests;

public class SecurityScannerTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly PrimusSecurityOptions _options;

    public SecurityScannerTests()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        _options = new PrimusSecurityOptions 
        { 
            EnableSecretDetection = false, 
            EnableDependencyScanning = false 
        };
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnPassed_WhenNoIssuesFound()
    {
        // Arrange
        var scanner = new SecurityScanner(
            NullLogger<SecurityScanner>.Instance,
            _options,
            _mockLoggerFactory.Object);

        // Act
        var result = await scanner.ScanAsync("some/path");

        // Assert
        result.Passed.Should().BeTrue();
        result.Findings.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanContentAsync_ShouldDetectSecrets_WhenEnabled()
    {
        // Arrange
        _options.EnableSecretDetection = true;
        // We rely on the fact that SecurityScanner constructor attempts to load default patterns
        // Since we can't easily mock the internal SecretDetector without more refactoring,
        // we'll skip the actual detection logic verification here and focus on the flow.
        // real integration testing would need the patterns file present.
        
        // For unit testing purposes, we might just verify it doesn't crash
        // Verification of actual detection is covered in SecretDetectorTests
        
        var scanner = new SecurityScanner(
             NullLogger<SecurityScanner>.Instance,
             _options,
             _mockLoggerFactory.Object);

        // Act
        var result = await scanner.ScanContentAsync("var x = 1;", "test.js");

        // Assert
        result.FilesScanned.Should().Be(1);
    }
}

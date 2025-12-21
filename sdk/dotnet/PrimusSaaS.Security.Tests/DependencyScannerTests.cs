using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PrimusSaaS.Security.Core;
using PrimusSaaS.Security.Models;
using PrimusSaaS.Security.Scanners;

namespace PrimusSaaS.Security.Tests;

public class DependencyScannerTests
{
    private readonly Mock<IVulnerabilityProvider> _mockProvider;
    private readonly DependencyScanner _scanner;
    private readonly string _testDir;

    public DependencyScannerTests()
    {
        _mockProvider = new Mock<IVulnerabilityProvider>();
        _scanner = new DependencyScanner(
            NullLogger<DependencyScanner>.Instance,
            _mockProvider.Object);
        
        _testDir = Path.Combine(Path.GetTempPath(), "PrimusTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task ScanAsync_ShouldDetectNugetPackages()
    {
        // Arrange
        var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Vulnerable.Package"" Version=""1.0.0"" />
  </ItemGroup>
</Project>";
        File.WriteAllText(Path.Combine(_testDir, "test.csproj"), csprojContent);

        _mockProvider.Setup(p => p.CheckPackageAsync(It.IsAny<Dependency>()))
            .ReturnsAsync((Dependency d) => 
            {
                if (d.Name == "Vulnerable.Package" && d.Version == "1.0.0")
                {
                    return new List<SecurityFinding> 
                    { 
                        new SecurityFinding { Title = "Vulnerability Found", Severity = SecuritySeverity.High } 
                    };
                }
                return new List<SecurityFinding>();
            });

        // Act
        var findings = await _scanner.ScanAsync(_testDir);

        // Assert
        findings.Should().HaveCount(1);
        findings.First().Title.Should().Be("Vulnerability Found");
        
        // Cleanup
        Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task ScanAsync_ShouldHandleNpmPackages()
    {
        // Arrange
        var packageJson = @"
{
  ""dependencies"": {
    ""vulnerable-lib"": ""1.0.0""
  }
}";
        File.WriteAllText(Path.Combine(_testDir, "package.json"), packageJson);

        _mockProvider.Setup(p => p.CheckPackageAsync(It.IsAny<Dependency>()))
             .ReturnsAsync(new List<SecurityFinding>());

        // Act
        await _scanner.ScanAsync(_testDir);

        // Assert
        _mockProvider.Verify(p => p.CheckPackageAsync(It.Is<Dependency>(d => 
            d.Name == "vulnerable-lib" && 
            d.Version == "1.0.0" && 
            d.Ecosystem == "npm")), Times.Once);
            
        // Cleanup
        Directory.Delete(_testDir, true);
    }
}

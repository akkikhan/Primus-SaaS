using Microsoft.Extensions.Logging.Abstractions;
using PrimusSaaS.Security.Data;
using PrimusSaaS.Security.Detectors;
using System.Reflection;
using Xunit.Abstractions;

namespace PrimusSaaS.Security.Tests;

public class SecretDetectorTests
{
    private readonly ITestOutputHelper _output;
    private readonly SecretDetector _detector;

    public SecretDetectorTests(ITestOutputHelper output)
    {
        _output = output;
        var provider = new FileSecretPatternProvider(NullLogger<FileSecretPatternProvider>.Instance);
        _detector = new SecretDetector(NullLogger<SecretDetector>.Instance, provider);
    }

    [Fact]
    public void ShouldDetectAwsAccessKey()
    {
        var content = "var key = \"AKIAIOSFODNN7EXAMPLE\";";
        var fileName = "TestFile.cs";

        var findings = _detector.Scan(content, fileName);

        if (!findings.Any())
        {
            var assembly = typeof(FileSecretPatternProvider).Assembly;
            var resources = string.Join(", ", assembly.GetManifestResourceNames());
            _output.WriteLine($"Available resources: {resources}");
            throw new Exception($"No findings. Patterns loaded? Available resources: {resources}");
        }

        Assert.NotEmpty(findings);
        Assert.Contains(findings, f => f.Title.Contains("AWS"));
    }

    [Fact]
    public void ShouldIgnoreLowEntropyString()
    {
        var content = "AKIAAAAAAAAAAAAAAAAA"; 
        var fileName = "TestFile.cs";

        var findings = _detector.Scan(content, fileName);

        Assert.Empty(findings);
    }
}

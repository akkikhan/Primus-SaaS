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
        // AKIAAAAAAAAAAAAAAAAA has entropy ~1.0 (only 2 unique chars: A, I, K)
        // With entropy_threshold: 3.0 on SEC001, this should be filtered out
        var content = "AKIAAAAAAAAAAAAAAAAA"; 
        var fileName = "TestFile.cs";

        var findings = _detector.Scan(content, fileName);

        // After adding entropy threshold to AWS pattern, low-entropy strings are filtered
        Assert.Empty(findings);
    }

    [Fact]
    public void ShouldDetectHighEntropyAwsKey()
    {
        // Real-looking AWS key with high entropy (many unique characters)
        var content = "var key = \"AKIAIOSFODNN7EXAMPLE\";";
        var fileName = "TestFile.cs";

        var findings = _detector.Scan(content, fileName);

        Assert.NotEmpty(findings);
        Assert.Contains(findings, f => f.Title.Contains("AWS"));
    }
}

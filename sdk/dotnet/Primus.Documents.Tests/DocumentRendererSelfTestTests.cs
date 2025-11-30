using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Primus.Documents.SelfTest;
using Xunit;

namespace Primus.Documents.Tests;

public class DocumentRendererSelfTestTests
{
    private readonly IDocumentRendererSelfTest _selfTest;

    public DocumentRendererSelfTestTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<DocumentRendererOptions>(opts =>
        {
            opts.MaxContentLength = 20000;
            opts.SelfTestEnabled = true;
        });
        services.AddPrimusDocumentRenderer();
        
        var provider = services.BuildServiceProvider();
        _selfTest = provider.GetRequiredService<IDocumentRendererSelfTest>();
    }

    [Fact]
    public async Task RunAsync_BasicMode_AllTestsPass()
    {
        // Act
        var result = await _selfTest.RunAsync(SelfTestMode.Basic);

        // Assert
        result.Success.Should().BeTrue();
        result.Mode.Should().Be(SelfTestMode.Basic);
        result.TotalTests.Should().BeGreaterThan(0);
        result.PassedTests.Should().Be(result.TotalTests);
        result.FailedTests.Should().Be(0);
        result.TestCases.Should().NotBeEmpty();
        result.TestCases.All(tc => tc.Category == "Basic").Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_ValidationMode_AllTestsPass()
    {
        // Act
        var result = await _selfTest.RunAsync(SelfTestMode.Validation);

        // Assert
        result.Success.Should().BeTrue();
        result.Mode.Should().Be(SelfTestMode.Validation);
        result.TotalTests.Should().BeGreaterThan(0);
        result.PassedTests.Should().Be(result.TotalTests);
        result.TestCases.All(tc => tc.Category == "Validation").Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_ComplexityMode_AllTestsPass()
    {
        // Act
        var result = await _selfTest.RunAsync(SelfTestMode.Complexity);

        // Assert
        result.Success.Should().BeTrue();
        result.Mode.Should().Be(SelfTestMode.Complexity);
        result.TotalTests.Should().BeGreaterThan(0);
        result.PassedTests.Should().Be(result.TotalTests);
        result.TestCases.All(tc => tc.Category == "Complexity").Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_FullMode_RunsAllCategories()
    {
        // Act
        var result = await _selfTest.RunAsync(SelfTestMode.Full);

        // Assert
        result.Success.Should().BeTrue();
        result.Mode.Should().Be(SelfTestMode.Full);
        result.TotalTests.Should().BeGreaterThan(10); // Should run many tests
        result.PassedTests.Should().Be(result.TotalTests);
        
        // Should include tests from all categories
        result.TestCases.Should().Contain(tc => tc.Category == "Basic");
        result.TestCases.Should().Contain(tc => tc.Category == "Validation");
        result.TestCases.Should().Contain(tc => tc.Category == "Complexity");
    }

    [Fact]
    public async Task RunAsync_ReturnsTimingInformation()
    {
        // Act
        var result = await _selfTest.RunAsync(SelfTestMode.Basic);

        // Assert
        result.DurationMs.Should().BeGreaterOrEqualTo(0);
        result.TestCases.All(tc => tc.DurationMs >= 0).Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_TestCasesHaveDetails()
    {
        // Act
        var result = await _selfTest.RunAsync(SelfTestMode.Basic);

        // Assert
        result.TestCases.Should().AllSatisfy(tc =>
        {
            tc.Name.Should().NotBeNullOrEmpty();
            tc.Category.Should().NotBeNullOrEmpty();
            if (tc.Passed)
            {
                tc.Details.Should().NotBeNullOrEmpty();
                tc.Error.Should().BeNull();
            }
        });
    }

    [Fact]
    public async Task RunAsync_GeneratesSummary()
    {
        // Act
        var result = await _selfTest.RunAsync(SelfTestMode.Basic);

        // Assert
        result.Summary.Should().NotBeNullOrEmpty();
        result.Summary.Should().Contain("Self-test");
        result.Summary.Should().Contain("passed");
    }
}

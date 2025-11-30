using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace LiveDemoApi.Tests.GoldenPath;

/// <summary>
/// Golden Path Tests for Feature Flags Module.
/// Validates the /feature-flags/test and /feature-flags/{flagName} endpoints work correctly.
/// </summary>
public class FeatureFlagsGoldenPathTests : IClassFixture<LiveDemoApiFactory>
{
    private readonly HttpClient _client;

    public FeatureFlagsGoldenPathTests(LiveDemoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FeatureFlagsTest_Endpoint_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/feature-flags/test");

        // Assert - Golden Path validates feature flags endpoint works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FeatureFlagsTest_Endpoint_ReturnsAllFlags()
    {
        // Act
        var response = await _client.GetAsync("/feature-flags/test");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Response should contain flags information
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrWhiteSpace();
        content.Should().Contain("flags");
        content.Should().Contain("success");
    }

    [Fact]
    public async Task FeatureFlagsTest_Endpoint_ReturnsConfiguredFlags()
    {
        // Act
        var response = await _client.GetAsync("/feature-flags/test");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Should return the preconfigured flags from appsettings.json
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify at least one of our configured flags is present
        content.Should().Contain("NewDashboard");
    }

    [Fact]
    public async Task FeatureFlagsTest_Endpoint_ReturnsUserInfo()
    {
        // Act
        var response = await _client.GetAsync("/feature-flags/test");
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        doc.RootElement.GetProperty("user").GetProperty("authenticated").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("user").GetProperty("userId").GetString().Should().Be("anonymous");
    }

    [Fact]
    public async Task GetSpecificFlag_WhenExists_ReturnsFlag()
    {
        // Act - NewDashboard is configured in appsettings.json
        var response = await _client.GetAsync("/feature-flags/NewDashboard");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("NewDashboard");
        content.Should().Contain("enabled");
    }

    [Fact]
    public async Task GetSpecificFlag_WhenNotExists_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/feature-flags/NonExistentFlag");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSpecificFlag_IsCaseInsensitive()
    {
        // Act - Test with different casing
        var response1 = await _client.GetAsync("/feature-flags/newdashboard");
        var response2 = await _client.GetAsync("/feature-flags/NEWDASHBOARD");
        var response3 = await _client.GetAsync("/feature-flags/NewDashboard");

        // Assert - All should succeed
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSpecificFlag_ReturnsFlagDefinition()
    {
        // Act
        var response = await _client.GetAsync("/feature-flags/NewDashboard");
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("flagName").GetString().Should().Be("NewDashboard");
        doc.RootElement.TryGetProperty("definition", out var definition).Should().BeTrue();
        definition.TryGetProperty("description", out _).Should().BeTrue();
    }

    [Fact]
    public async Task FeatureFlagsTest_ReturnsSummary()
    {
        // Act
        var response = await _client.GetAsync("/feature-flags/test");
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        doc.RootElement.TryGetProperty("summary", out var summary).Should().BeTrue();
        summary.TryGetProperty("totalFlags", out var totalFlags).Should().BeTrue();
        totalFlags.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FeatureFlagsTest_ReturnsTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/feature-flags/test");
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        doc.RootElement.TryGetProperty("timestamp", out var timestamp).Should().BeTrue();
        timestamp.GetString().Should().NotBeNullOrWhiteSpace();
    }
}

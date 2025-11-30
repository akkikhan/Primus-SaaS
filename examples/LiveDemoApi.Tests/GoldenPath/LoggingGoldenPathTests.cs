using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace LiveDemoApi.Tests.GoldenPath;

/// <summary>
/// Golden Path Tests for Logging Module.
/// Validates the /log/test endpoint creates structured logs correctly.
/// </summary>
public class LoggingGoldenPathTests : IClassFixture<LiveDemoApiFactory>
{
    private readonly HttpClient _client;

    public LoggingGoldenPathTests(LiveDemoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LogTest_Endpoint_ReturnsSuccess()
    {
        // Arrange - Endpoint requires a JSON body
        var request = new { message = "Test log message" };

        // Act
        var response = await _client.PostAsJsonAsync("/log/test", request);

        // Assert - Golden Path validates logging endpoint works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogTest_Endpoint_ReturnsLogDetails()
    {
        // Arrange
        var request = new { message = "Test log message" };

        // Act
        var response = await _client.PostAsJsonAsync("/log/test", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Response should contain log confirmation
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrWhiteSpace();
        content.Should().Contain("logged");
    }

    [Fact]
    public async Task LogTest_WithCustomFields_ReturnsSuccess()
    {
        // Arrange
        var logRequest = new
        {
            message = "Golden Path test log",
            userId = Guid.NewGuid().ToString(),
            level = "Information"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/log/test", logRequest);

        // Assert - Custom fields logging should work
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogTest_MultipleRequests_AllSucceed()
    {
        // Arrange & Act - Send multiple log requests with bodies
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _client.PostAsJsonAsync("/log/test", new { message = "Batch test" }))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should succeed
        responses.Should().AllSatisfy(r => 
            r.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [Fact]
    public async Task LogTest_Response_DoesNotContainSecrets()
    {
        // Arrange
        var request = new { message = "Security test log" };

        // Act
        var response = await _client.PostAsJsonAsync("/log/test", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Response should not leak secrets or tokens
        content.Should().NotContain("secret", because: "secrets should not be logged");
        content.Should().NotContain("Bearer ", because: "tokens should not be logged");
        content.Should().NotContain("connectionString", because: "connection strings should not be exposed");
    }
}

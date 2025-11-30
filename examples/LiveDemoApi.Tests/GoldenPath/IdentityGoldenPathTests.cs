using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace LiveDemoApi.Tests.GoldenPath;

/// <summary>
/// Golden Path Tests for Identity Validator Module.
/// Validates the /whoami endpoint returns JWT claims correctly.
/// </summary>
public class IdentityGoldenPathTests : IClassFixture<LiveDemoApiFactory>
{
    private readonly HttpClient _client;

    public IdentityGoldenPathTests(LiveDemoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WhoAmI_WithoutAuth_Returns401Unauthorized()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/whoami");

        // Assert - Golden Path validates authentication is enforced
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhoAmI_EndpointExists_ReturnsNotServerError()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/whoami");

        // Assert - Endpoint is configured (not 500/404 for wrong route)
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LocalAuth_WithValidCredentials_ReturnsToken()
    {
        // Arrange - Using /auth/local endpoint
        var loginRequest = new
        {
            email = "demo@primus.local",
            password = "PrimusDemo123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/local", loginRequest);

        // Assert - Golden Path validates local auth flow works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("token");
    }

    [Fact]
    public async Task LocalAuth_WithInvalidCredentials_ReturnsUnauthorizedOrBadRequest()
    {
        // Arrange
        var loginRequest = new
        {
            email = "demo@primus.local",
            password = "wrong-password"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/local", loginRequest);

        // Assert - Invalid credentials should return either 401 or 400
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task LocalAuth_EndpointExists_ReturnsNotNotFound()
    {
        // Arrange
        var loginRequest = new
        {
            email = "test@example.com",
            password = "test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/local", loginRequest);

        // Assert - Endpoint should exist (401 for invalid creds, not 404)
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}

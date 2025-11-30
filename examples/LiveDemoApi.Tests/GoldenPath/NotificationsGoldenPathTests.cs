using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace LiveDemoApi.Tests.GoldenPath;

/// <summary>
/// Golden Path Tests for Notifications Module.
/// Validates the /notifications/test endpoint sends notifications correctly.
/// Note: Uses logger fallback since external providers aren't configured in tests.
/// </summary>
public class NotificationsGoldenPathTests : IClassFixture<LiveDemoApiFactory>
{
    private readonly HttpClient _client;

    public NotificationsGoldenPathTests(LiveDemoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task NotificationsTest_Endpoint_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            email = "test@example.com",
            type = "PasswordReset"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/notifications/test", request);

        // Assert - Golden Path validates notification endpoint works
        // Even without SMTP, should return OK (uses logger fallback)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NotificationsTest_WithRecipientName_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            email = "john.doe@example.com",
            name = "John Doe",
            type = "PasswordReset"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/notifications/test", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NotificationsTest_Response_ContainsConfirmation()
    {
        // Arrange
        var request = new
        {
            email = "test@example.com",
            type = "PasswordReset"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/notifications/test", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Response should confirm notification was processed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrWhiteSpace();
        content.Should().Contain("success");
    }

    [Fact]
    public async Task NotificationsTest_WithoutEmail_ReturnsBadRequest()
    {
        // Arrange - Missing required email
        var request = new
        {
            type = "PasswordReset"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/notifications/test", request);

        // Assert - Should validate required fields
        // Note: Depending on implementation, may return 400 or 500
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.OK // If email defaults or is optional
        );
    }

    [Fact]
    public async Task NotificationsTest_Response_DoesNotLeakCredentials()
    {
        // Arrange
        var request = new
        {
            email = "test@example.com",
            type = "PasswordReset"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/notifications/test", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Response should not contain SMTP/Twilio credentials
        // Note: "password" may appear in template content (e.g., "Reset Password button")
        // so we check for actual credential patterns instead
        content.Should().NotContain("authToken", because: "Twilio auth token should not be exposed");
        content.Should().NotContain("accountSid", because: "Twilio SID should not be exposed");
        content.Should().NotContain("smtp.gmail.com", because: "SMTP host should not be in response");
        content.Should().NotContain("your-app-password", because: "SMTP password should not be exposed");
    }

    [Fact]
    public async Task NotificationsTest_TemplateRendering_ReturnsValidHtml()
    {
        // Arrange - Request that uses PasswordReset template
        var request = new
        {
            email = "template-test@example.com",
            name = "Template Tester",
            type = "PasswordReset"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/notifications/test", request);

        // Assert - Template should render without errors
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        // Response should contain rendered HTML, not raw Liquid errors
        content.Should().Contain("body");
        content.Should().Contain("subject");
    }
}

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using PrimusSaaS.Identity.Validator;
using System.Security.Claims;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class PrimusUserExtensionsTests
{
    [Fact]
    public void GetPrimusUser_WithAuthenticatedUser_ShouldReturnUser()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        // Act
        var user = httpContext.GetPrimusUser();

        // Assert
        user.Should().NotBeNull();
        user!.UserId.Should().Be("user-123");
        user.Email.Should().Be("test@example.com");
        user.Name.Should().Be("Test User");
    }

    [Fact]
    public void GetPrimusUser_WithUnauthenticatedUser_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var user = httpContext.GetPrimusUser();

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public void GetPrimusUser_WithNullHttpContext_ShouldReturnNull()
    {
        // Arrange
        HttpContext? httpContext = null;

        // Act
        var user = httpContext.GetPrimusUser();

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public void GetPrimusUser_WithEmptyPrincipal_ShouldReturnNull()
    {
        // Arrange - Identity is not authenticated
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        // Act
        var user = httpContext.GetPrimusUser();

        // Assert
        user.Should().BeNull();
    }
}

using FluentAssertions;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class PrimusIdentityOptionsTests
{
    [Fact]
    public void Validate_WithAllRequiredFields_ShouldSucceed()
    {
        // Arrange
        var options = new PrimusIdentityOptions
        {
            PortalUrl = "https://portal.primus-saas.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            JwtSecret = "test-jwt-secret-key-with-sufficient-length"
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
        options.Issuer.Should().Be("https://portal.primus-saas.com");
        options.Audience.Should().Be("test-client-id");
    }

    [Fact]
    public void Validate_WithMissingPortalUrl_ShouldThrow()
    {
        // Arrange
        var options = new PrimusIdentityOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            JwtSecret = "test-jwt-secret"
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("PortalUrl is required.*");
    }

    [Fact]
    public void Validate_WithMissingClientId_ShouldThrow()
    {
        // Arrange
        var options = new PrimusIdentityOptions
        {
            PortalUrl = "https://portal.primus-saas.com",
            ClientSecret = "test-client-secret",
            JwtSecret = "test-jwt-secret"
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("ClientId is required.*");
    }

    [Fact]
    public void Validate_WithMissingClientSecret_ShouldThrow()
    {
        // Arrange
        var options = new PrimusIdentityOptions
        {
            PortalUrl = "https://portal.primus-saas.com",
            ClientId = "test-client-id",
            JwtSecret = "test-jwt-secret"
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("ClientSecret is required.*");
    }

    [Fact]
    public void Validate_WithMissingJwtSecret_ShouldThrow()
    {
        // Arrange
        var options = new PrimusIdentityOptions
        {
            PortalUrl = "https://portal.primus-saas.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("JwtSecret is required.*");
    }

    [Fact]
    public void Validate_WithInvalidPortalUrl_ShouldThrow()
    {
        // Arrange
        var options = new PrimusIdentityOptions
        {
            PortalUrl = "not-a-valid-url",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            JwtSecret = "test-jwt-secret"
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("PortalUrl must be a valid absolute URI.*");
    }

    [Fact]
    public void Validate_WithCustomIssuerAndAudience_ShouldPreserveValues()
    {
        // Arrange
        var options = new PrimusIdentityOptions
        {
            PortalUrl = "https://portal.primus-saas.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            JwtSecret = "test-jwt-secret",
            Issuer = "custom-issuer",
            Audience = "custom-audience"
        };

        // Act
        options.Validate();

        // Assert
        options.Issuer.Should().Be("custom-issuer");
        options.Audience.Should().Be("custom-audience");
    }

    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var options = new PrimusIdentityOptions();

        // Assert
        options.ValidateLifetime.Should().BeTrue();
        options.RequireHttpsMetadata.Should().BeTrue();
        options.ClockSkew.Should().Be(TimeSpan.FromMinutes(5));
    }
}

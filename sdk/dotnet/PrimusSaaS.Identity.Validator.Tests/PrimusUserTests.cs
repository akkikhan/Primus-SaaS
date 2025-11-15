using FluentAssertions;
using PrimusSaaS.Identity.Validator;
using System.Security.Claims;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class PrimusUserTests
{
    [Fact]
    public void FromClaimsPrincipal_WithAllClaims_ShouldCreateUserCorrectly()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim("custom-claim", "custom-value")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var user = PrimusUser.FromClaimsPrincipal(principal);

        // Assert
        user.Should().NotBeNull();
        user.UserId.Should().Be("user-123");
        user.Email.Should().Be("test@example.com");
        user.Name.Should().Be("Test User");
        user.Roles.Should().HaveCount(2);
        user.Roles.Should().Contain("Admin");
        user.Roles.Should().Contain("User");
        user.AdditionalClaims.Should().ContainKey("custom-claim");
        user.AdditionalClaims["custom-claim"].Should().Be("custom-value");
    }

    [Fact]
    public void FromClaimsPrincipal_WithMissingClaims_ShouldReturnEmptyStrings()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var user = PrimusUser.FromClaimsPrincipal(principal);

        // Assert
        user.UserId.Should().BeEmpty();
        user.Email.Should().BeEmpty();
        user.Name.Should().BeEmpty();
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void FromClaimsPrincipal_WithNullPrincipal_ShouldThrow()
    {
        // Arrange
        ClaimsPrincipal principal = null!;

        // Act
        Action act = () => PrimusUser.FromClaimsPrincipal(principal);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromClaimsPrincipal_WithMultipleAdditionalClaims_ShouldCaptureAll()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim("tenant", "tenant-abc"),
            new Claim("subscription", "premium"),
            new Claim("region", "us-east")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var user = PrimusUser.FromClaimsPrincipal(principal);

        // Assert
        user.AdditionalClaims.Should().HaveCount(3);
        user.AdditionalClaims["tenant"].Should().Be("tenant-abc");
        user.AdditionalClaims["subscription"].Should().Be("premium");
        user.AdditionalClaims["region"].Should().Be("us-east");
    }

    [Fact]
    public void FromClaimsPrincipal_WithNoRoles_ShouldReturnEmptyRolesList()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var user = PrimusUser.FromClaimsPrincipal(principal);

        // Assert
        user.Roles.Should().BeEmpty();
    }
}

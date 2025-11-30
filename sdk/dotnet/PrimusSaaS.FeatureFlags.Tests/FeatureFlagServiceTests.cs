using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PrimusSaaS.FeatureFlags;
using PrimusSaaS.FeatureFlags.Providers;
using Xunit;

namespace PrimusSaaS.FeatureFlags.Tests;

public class FeatureFlagServiceTests
{
    private readonly Mock<IOptionsMonitor<FeatureFlagsOptions>> _optionsMonitor;
    private readonly FeatureFlagsOptions _options;

    public FeatureFlagServiceTests()
    {
        _options = new FeatureFlagsOptions
        {
            DefaultValue = false,
            Logging = new FeatureFlagLoggingOptions { LogEvaluations = false }
        };

        _optionsMonitor = new Mock<IOptionsMonitor<FeatureFlagsOptions>>();
        _optionsMonitor.Setup(x => x.CurrentValue).Returns(_options);
    }

    private IFeatureFlagService CreateService()
    {
        var provider = new InMemoryFeatureFlagProvider(_optionsMonitor.Object);
        var optionsMock = new Mock<IOptions<FeatureFlagsOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);
        
        return new FeatureFlagService(
            optionsMock.Object,
            NullLogger<FeatureFlagService>.Instance,
            provider);
    }

    [Fact]
    public void IsEnabled_WhenFlagNotFound_ReturnsDefaultValue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsEnabled("NonExistentFlag");

        // Assert
        result.Should().Be(_options.DefaultValue);
    }

    [Fact]
    public void IsEnabled_WhenFlagEnabled_ReturnsTrue()
    {
        // Arrange
        _options.Flags["TestFeature"] = new FeatureFlagDefinition { Enabled = true };
        var service = CreateService();

        // Act
        var result = service.IsEnabled("TestFeature");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WhenFlagDisabled_ReturnsFalse()
    {
        // Arrange
        _options.Flags["TestFeature"] = new FeatureFlagDefinition { Enabled = false };
        var service = CreateService();

        // Act
        var result = service.IsEnabled("TestFeature");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_WithUserTargeting_ReturnsTrue()
    {
        // Arrange
        _options.Flags["BetaFeature"] = new FeatureFlagDefinition
        {
            Enabled = false,
            EnabledForUsers = new List<string> { "user123", "user456" }
        };
        var service = CreateService();

        // Act
        var result = service.IsEnabled("BetaFeature", "user123");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WithUserNotTargeted_ReturnsFalse()
    {
        // Arrange
        _options.Flags["BetaFeature"] = new FeatureFlagDefinition
        {
            Enabled = false,
            EnabledForUsers = new List<string> { "user123", "user456" }
        };
        var service = CreateService();

        // Act
        var result = service.IsEnabled("BetaFeature", "user789");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_WithGroupTargeting_ReturnsTrue()
    {
        // Arrange
        _options.Flags["AdminFeature"] = new FeatureFlagDefinition
        {
            Enabled = false,
            EnabledForGroups = new List<string> { "admins", "moderators" }
        };
        var service = CreateService();
        var context = new FeatureFlagContext
        {
            UserId = "user123",
            Groups = new List<string> { "users", "admins" }
        };

        // Act
        var result = service.IsEnabled("AdminFeature", context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WithGroupNotTargeted_ReturnsFalse()
    {
        // Arrange
        _options.Flags["AdminFeature"] = new FeatureFlagDefinition
        {
            Enabled = false,
            EnabledForGroups = new List<string> { "admins", "moderators" }
        };
        var service = CreateService();
        var context = new FeatureFlagContext
        {
            UserId = "user123",
            Groups = new List<string> { "users", "guests" }
        };

        // Act
        var result = service.IsEnabled("AdminFeature", context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_WithClaimsPrincipal_ExtractsUserIdFromClaims()
    {
        // Arrange
        _options.Flags["UserFeature"] = new FeatureFlagDefinition
        {
            Enabled = false,
            EnabledForUsers = new List<string> { "user-sub-123" }
        };
        var service = CreateService();
        
        var claims = new List<Claim>
        {
            new Claim("sub", "user-sub-123"),
            new Claim(ClaimTypes.Email, "user@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = service.IsEnabled("UserFeature", principal);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_BeforeStartTime_ReturnsFalse()
    {
        // Arrange
        _options.Flags["FutureFeature"] = new FeatureFlagDefinition
        {
            Enabled = true,
            StartTime = DateTimeOffset.UtcNow.AddDays(1)
        };
        var service = CreateService();

        // Act
        var result = service.IsEnabled("FutureFeature");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_AfterEndTime_ReturnsFalse()
    {
        // Arrange
        _options.Flags["ExpiredFeature"] = new FeatureFlagDefinition
        {
            Enabled = true,
            EndTime = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var service = CreateService();

        // Act
        var result = service.IsEnabled("ExpiredFeature");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_WithinTimeWindow_ReturnsEnabled()
    {
        // Arrange
        _options.Flags["ActiveFeature"] = new FeatureFlagDefinition
        {
            Enabled = true,
            StartTime = DateTimeOffset.UtcNow.AddDays(-1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1)
        };
        var service = CreateService();

        // Act
        var result = service.IsEnabled("ActiveFeature");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WithRolloutPercentage100_AlwaysReturnsTrue()
    {
        // Arrange
        _options.Flags["RolloutFeature"] = new FeatureFlagDefinition
        {
            Enabled = false,
            RolloutPercentage = 100
        };
        var service = CreateService();

        // Act & Assert
        for (int i = 0; i < 10; i++)
        {
            var result = service.IsEnabled("RolloutFeature", $"user{i}");
            result.Should().BeTrue();
        }
    }

    [Fact]
    public void IsEnabled_WithRolloutPercentage0_AlwaysReturnsFalse()
    {
        // Arrange
        _options.Flags["RolloutFeature"] = new FeatureFlagDefinition
        {
            Enabled = false,
            RolloutPercentage = 0
        };
        var service = CreateService();

        // Act & Assert
        for (int i = 0; i < 10; i++)
        {
            var result = service.IsEnabled("RolloutFeature", $"user{i}");
            result.Should().BeFalse();
        }
    }

    [Fact]
    public void IsEnabled_WithRolloutPercentage_IsConsistentForSameUser()
    {
        // Arrange
        _options.Flags["RolloutFeature"] = new FeatureFlagDefinition
        {
            Enabled = false,
            RolloutPercentage = 50
        };
        var service = CreateService();

        // Act
        var results = Enumerable.Range(0, 10)
            .Select(_ => service.IsEnabled("RolloutFeature", "consistent-user"))
            .ToList();

        // Assert - all results should be the same for the same user
        results.Should().AllBeEquivalentTo(results.First());
    }

    [Fact]
    public async Task GetAllFlagsAsync_ReturnsAllFlags()
    {
        // Arrange
        _options.Flags["Feature1"] = new FeatureFlagDefinition { Enabled = true };
        _options.Flags["Feature2"] = new FeatureFlagDefinition { Enabled = false };
        _options.Flags["Feature3"] = new FeatureFlagDefinition { Enabled = true };
        var service = CreateService();

        // Act
        var flags = await service.GetAllFlagsAsync();

        // Assert
        flags.Should().HaveCount(3);
        flags["Feature1"].Should().BeTrue();
        flags["Feature2"].Should().BeFalse();
        flags["Feature3"].Should().BeTrue();
    }

    [Fact]
    public async Task GetFlagDefinitionAsync_WhenExists_ReturnsDefinition()
    {
        // Arrange
        _options.Flags["TestFeature"] = new FeatureFlagDefinition
        {
            Enabled = true,
            Description = "Test description",
            RolloutPercentage = 75
        };
        var service = CreateService();

        // Act
        var definition = await service.GetFlagDefinitionAsync("TestFeature");

        // Assert
        definition.Should().NotBeNull();
        definition!.Enabled.Should().BeTrue();
        definition.Description.Should().Be("Test description");
        definition.RolloutPercentage.Should().Be(75);
    }

    [Fact]
    public async Task GetFlagDefinitionAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var definition = await service.GetFlagDefinitionAsync("NonExistent");

        // Assert
        definition.Should().BeNull();
    }

    [Fact]
    public void IsEnabled_IsCaseInsensitive()
    {
        // Arrange
        _options.Flags["TestFeature"] = new FeatureFlagDefinition { Enabled = true };
        var service = CreateService();

        // Act & Assert
        service.IsEnabled("testfeature").Should().BeTrue();
        service.IsEnabled("TESTFEATURE").Should().BeTrue();
        service.IsEnabled("TestFeature").Should().BeTrue();
    }
}

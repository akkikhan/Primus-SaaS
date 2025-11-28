using FluentAssertions;
using System.Security.Claims;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class TokenClassificationTests
{
    [Fact]
    public void IsMachineToMachine_DetectsClientCredentialsGrant()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("gty", "client-credentials"),
            new Claim("sub", "client@clients")
        }, "test"));

        TokenClassification.IsMachineToMachine(principal).Should().BeTrue();
    }

    [Fact]
    public void IsMachineToMachine_DetectsClientsSuffix()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "client-id@clients")
        }, "test"));

        TokenClassification.IsMachineToMachine(principal).Should().BeTrue();
    }

    [Fact]
    public void ValidateMachineToMachineAllowed_FailsWhenNotAllowed()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("gty", "client-credentials")
        }, "test"));

        var issuer = new IssuerConfig { AllowMachineToMachine = false };

        TokenClassification.ValidateMachineToMachineAllowed(principal, issuer, out var error).Should().BeFalse();
        error.Should().Contain("not allowed");
    }

    [Fact]
    public void ValidateMachineToMachineAllowed_AllowsWhenGrantTypeMatches()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("gty", "client-credentials")
        }, "test"));

        var issuer = new IssuerConfig
        {
            AllowMachineToMachine = true,
            AllowedGrantTypes = new List<string> { "client-credentials" }
        };

        TokenClassification.ValidateMachineToMachineAllowed(principal, issuer, out var error).Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateMachineToMachineAllowed_FailsWhenGrantTypeNotAllowed()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("gty", "client-credentials")
        }, "test"));

        var issuer = new IssuerConfig
        {
            AllowMachineToMachine = true,
            AllowedGrantTypes = new List<string> { "custom-grant" }
        };

        TokenClassification.ValidateMachineToMachineAllowed(principal, issuer, out var error).Should().BeFalse();
        error.Should().Contain("Grant type");
    }

    [Fact]
    public void ValidateMachineToMachineAllowed_FailsWhenScopeDisallowed()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("gty", "client-credentials"),
            new Claim("scope", "read:clients write:clients")
        }, "test"));

        var issuer = new IssuerConfig
        {
            AllowMachineToMachine = true,
            AllowedGrantTypes = new List<string> { "client-credentials" },
            AllowedMachineToMachineScopes = new List<string> { "read:clients" } // disallow write
        };

        TokenClassification.ValidateMachineToMachineAllowed(principal, issuer, out var error).Should().BeFalse();
        error.Should().Contain("disallowed scopes");
    }

    [Fact]
    public void ValidateMachineToMachineAllowed_AllowsWhenScopesWithinAllowed()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("gty", "client-credentials"),
            new Claim("scope", "read:clients")
        }, "test"));

        var issuer = new IssuerConfig
        {
            AllowMachineToMachine = true,
            AllowedGrantTypes = new List<string> { "client-credentials" },
            AllowedMachineToMachineScopes = new List<string> { "read:clients", "write:clients" }
        };

        TokenClassification.ValidateMachineToMachineAllowed(principal, issuer, out var error).Should().BeTrue();
        error.Should().BeNull();
    }
}

using System.Security.Claims;
using FluentAssertions;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class Auth0OptionsTests
{
    [Fact]
    public void ToIssuerConfig_BuildsIssuerAuthorityAndMappings()
    {
        var options = new Auth0Options
        {
            Domain = "demo.auth0.com",
            RoleClaimName = "https://api.demo.com/roles"
        };
        options.Audiences.Add("https://api.demo.com");
        options.AllowMachineToMachine = true;
        options.AllowedGrantTypes.Add("client-credentials");
        options.AllowedMachineToMachineScopes.Add("read:clients");
        options.RequireEmailVerification = true;

        var config = options.ToIssuerConfig();

        config.Type.Should().Be(IssuerType.Auth0);
        config.Authority.Should().Be("https://demo.auth0.com");
        config.Issuer.Should().Be("https://demo.auth0.com/");
        config.Audiences.Should().Contain("https://api.demo.com");
        config.RoleClaimName.Should().Be("https://api.demo.com/roles");
        config.PermissionClaimName.Should().Be(PrimusClaimTypes.Permission);
        config.OrganizationClaimName.Should().Be(PrimusClaimTypes.Organization);
        config.ClaimMappings.Should().ContainKey("sub").WhoseValue.Should().Be(ClaimTypes.NameIdentifier);
        config.ClaimMappings.Should().ContainKey("email").WhoseValue.Should().Be(ClaimTypes.Email);
        config.ClaimMappings.Should().ContainKey("name").WhoseValue.Should().Be(ClaimTypes.Name);
        config.AllowMachineToMachine.Should().BeTrue();
        config.AllowedGrantTypes.Should().Contain("client-credentials");
        config.AllowedMachineToMachineScopes.Should().Contain("read:clients");
        config.RequireEmailVerification.Should().BeTrue();
    }

    [Fact]
    public void UseAuth0_AddsIssuerConfigToOptions()
    {
        var identityOptions = new PrimusIdentityOptions();

        identityOptions.UseAuth0("demo.auth0.com", "https://api.demo.com", auth0 =>
        {
            auth0.ValidateOrganization = true;
            auth0.RequiredOrganization = "org_123";
        });

        identityOptions.Issuers.Should().HaveCount(1);
        var issuer = identityOptions.Issuers.Single();
        issuer.Name.Should().Be("Auth0");
        issuer.ValidateOrganization.Should().BeTrue();
        issuer.RequiredOrganization.Should().Be("org_123");
        issuer.Audiences.Should().Contain("https://api.demo.com");
    }
}

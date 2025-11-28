using System.Security.Claims;
using FluentAssertions;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class GoogleOptionsTests
{
    [Fact]
    public void ToIssuerConfig_UsesGoogleIssuerAndAudience()
    {
        var options = new GoogleOptions();
        options.Audiences.Add("google-client-id");
        options.ClaimMappings["custom"] = "customMapped";

        var issuer = options.ToIssuerConfig();

        issuer.Type.Should().Be(IssuerType.Google);
        issuer.Issuer.Should().Be("https://accounts.google.com/");
        issuer.Authority.Should().Be("https://accounts.google.com");
        issuer.Audiences.Should().Contain("google-client-id");
        issuer.ClaimMappings.Should().ContainKey("sub").WhoseValue.Should().Be(ClaimTypes.NameIdentifier);
        issuer.ClaimMappings.Should().ContainKey("email").WhoseValue.Should().Be(ClaimTypes.Email);
    }

    [Fact]
    public void UseGoogle_AddsIssuer()
    {
        var identity = new PrimusIdentityOptions();
        identity.UseGoogle("google-client-id");

        identity.Issuers.Should().HaveCount(1);
        identity.Issuers.Single().Name.Should().Be("Google");
    }
}

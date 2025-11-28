using System.Security.Claims;
using FluentAssertions;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class CognitoOptionsTests
{
    [Fact]
    public void ToIssuerConfig_BuildsIssuerAuthorityAndMappings()
    {
        var options = new CognitoOptions
        {
            Region = "us-east-1",
            UserPoolId = "us-east-1_ABC123",
            RoleClaimName = "cognito:groups"
        };
        options.Audiences.Add("app-client-id");

        var config = options.ToIssuerConfig();

        config.Type.Should().Be(IssuerType.Cognito);
        config.Authority.Should().Be("https://cognito-idp.us-east-1.amazonaws.com/us-east-1_ABC123");
        config.Issuer.Should().Be("https://cognito-idp.us-east-1.amazonaws.com/us-east-1_ABC123");
        config.Audiences.Should().Contain("app-client-id");
        config.RoleClaimName.Should().Be("cognito:groups");
        config.ClaimMappings.Should().ContainKey("sub").WhoseValue.Should().Be(ClaimTypes.NameIdentifier);
        config.ClaimMappings.Should().ContainKey("email").WhoseValue.Should().Be(ClaimTypes.Email);
    }

    [Fact]
    public void UseCognito_AddsIssuer()
    {
        var identity = new PrimusIdentityOptions();
        identity.UseCognito("us-east-1", "us-east-1_ABC123", "app-client-id");

        identity.Issuers.Should().HaveCount(1);
        identity.Issuers.Single().Name.Should().Be("Cognito");
    }
}

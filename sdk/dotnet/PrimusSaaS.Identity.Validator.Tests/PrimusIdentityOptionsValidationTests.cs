using FluentAssertions;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class PrimusIdentityOptionsValidationTests
{
    [Fact]
    public void Validate_ShouldFail_WhenIssuerNotHttps()
    {
        var options = new PrimusIdentityOptions
        {
            Issuers = new List<IssuerConfig>
            {
                new IssuerConfig
                {
                    Name = "BadIssuer",
                    Type = IssuerType.Oidc,
                    Issuer = "http://demo.auth0.com/",
                    Authority = "http://demo.auth0.com",
                    Audiences = new List<string> { "https://api" }
                }
            }
        };

        Action act = options.Validate;

        act.Should().Throw<ArgumentException>()
            .WithMessage("*HTTPS URI*");
    }

    [Fact]
    public void Validate_ShouldFail_WhenOrgRequiredButClaimNameMissing()
    {
        var options = new PrimusIdentityOptions
        {
            Issuers = new List<IssuerConfig>
            {
                new IssuerConfig
                {
                    Name = "Auth0",
                    Type = IssuerType.Oidc,
                    Issuer = "https://demo.auth0.com/",
                    Authority = "https://demo.auth0.com",
                    Audiences = new List<string> { "https://api" },
                    ValidateOrganization = true,
                    OrganizationClaimName = ""
                }
            }
        };

        Action act = options.Validate;

        act.Should().Throw<ArgumentException>()
            .WithMessage("*OrganizationClaimName*");
    }
}

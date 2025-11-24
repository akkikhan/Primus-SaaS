using FluentAssertions;

namespace PrimusSaaS.Identity.Validator.Tests;

public class UnitTest1
{
    [Fact]
    public void AzureAdIssuerType_Should_Validate_Like_Oidc()
    {
        var options = new PrimusIdentityOptions
        {
            Issuers = new List<IssuerConfig>
            {
                new IssuerConfig
                {
                    Name = "AzureAD",
                    Type = IssuerType.AzureAD,
                    Issuer = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
                    Authority = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
                    Audiences = new List<string> { "api://test" }
                }
            }
        };

        Action act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void AzureAdIssuerType_Requires_Authority()
    {
        var options = new PrimusIdentityOptions
        {
            Issuers = new List<IssuerConfig>
            {
                new IssuerConfig
                {
                    Name = "AzureAD",
                    Type = IssuerType.AzureAD,
                    Issuer = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
                    Audiences = new List<string> { "api://test" }
                }
            }
        };

        Action act = () => options.Validate();

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Authority URL is required*");
    }

    [Fact]
    public void DuplicateIssuerNames_Should_FailValidation()
    {
        var options = new PrimusIdentityOptions
        {
            Issuers = new List<IssuerConfig>
            {
                new IssuerConfig
                {
                    Name = "AzureAD",
                    Type = IssuerType.AzureAD,
                    Issuer = "https://login.microsoftonline.com/tenant/v2.0",
                    Authority = "https://login.microsoftonline.com/tenant",
                    Audiences = new List<string> { "api://test" }
                },
                new IssuerConfig
                {
                    Name = "AzureAD", // duplicate name
                    Type = IssuerType.Jwt,
                    Issuer = "https://local.issuer",
                    Secret = "secret",
                    Audiences = new List<string> { "api://local" }
                }
            }
        };

        Action act = () => options.Validate();

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Duplicate issuer name*");
    }

    [Fact]
    public void DuplicateIssuerClaim_Should_FailValidation()
    {
        var options = new PrimusIdentityOptions
        {
            Issuers = new List<IssuerConfig>
            {
                new IssuerConfig
                {
                    Name = "AzureAD",
                    Type = IssuerType.AzureAD,
                    Issuer = "https://login.microsoftonline.com/tenant/v2.0",
                    Authority = "https://login.microsoftonline.com/tenant",
                    Audiences = new List<string> { "api://test" }
                },
                new IssuerConfig
                {
                    Name = "Local",
                    Type = IssuerType.Jwt,
                    Issuer = "https://login.microsoftonline.com/tenant/v2.0", // duplicate claim value
                    Secret = "secret",
                    Audiences = new List<string> { "api://local" }
                }
            }
        };

        Action act = () => options.Validate();

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Duplicate issuer claim value*");
    }

    [Fact]
    public void OidcAuthority_MustBeAbsoluteHttps()
    {
        var options = new PrimusIdentityOptions
        {
            Issuers = new List<IssuerConfig>
            {
                new IssuerConfig
                {
                    Name = "AzureAD",
                    Type = IssuerType.AzureAD,
                    Issuer = "issuer",
                    Authority = "http://relative",
                    Audiences = new List<string> { "api://test" }
                }
            }
        };

        Action act = () => options.Validate();

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Authority must use HTTPS*");
    }
}

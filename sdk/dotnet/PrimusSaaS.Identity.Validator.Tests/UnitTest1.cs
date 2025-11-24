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
}

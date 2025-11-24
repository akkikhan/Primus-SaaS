using FluentAssertions;
using Microsoft.Extensions.Options;
using PrimusSaaS.Identity.Validator.Services;

namespace PrimusSaaS.Identity.Validator.Tests;

public class IdentityDiagnosticsServiceTests
{
    [Fact]
    public void GetSnapshot_ShouldIncludeIssuers_AndJwksDiagnostics()
    {
        // Arrange
        var options = new PrimusIdentityOptions
        {
            Issuers = new List<IssuerConfig>
            {
                new IssuerConfig
                {
                    Name = "AzureAD",
                    Type = IssuerType.AzureAD,
                    Authority = "https://login.microsoftonline.com/tenant",
                    Issuer = "https://login.microsoftonline.com/tenant/v2.0",
                    Audiences = new List<string> { "api://test" }
                },
                new IssuerConfig
                {
                    Name = "Local",
                    Type = IssuerType.Jwt,
                    Issuer = "https://local.issuer",
                    Secret = "secret",
                    Audiences = new List<string> { "api://local" }
                }
            }
        };

        var jwksDiagnostics = new JwksServiceDiagnostics
        {
            CacheHits = 2,
            CacheMisses = 1,
            FetchAttempts = 3,
            FetchFailures = 0,
            LastSuccessUtc = DateTimeOffset.UtcNow
        };

        var securityMetrics = new SecurityEventMetrics();
        securityMetrics.IncrementSuccess();
        var service = new IdentityDiagnosticsService(options, () => jwksDiagnostics, securityMetrics);

        // Act
        var snapshot = service.GetSnapshot();

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.Issuers.Should().HaveCount(2);
        snapshot.Issuers.First().Name.Should().Be("AzureAD");
        snapshot.Jwks.CacheHits.Should().Be(2);
        snapshot.Jwks.CacheMisses.Should().Be(1);
        snapshot.Jwks.FetchAttempts.Should().Be(3);
        snapshot.Jwks.FetchFailures.Should().Be(0);
        snapshot.Jwks.LastSuccessUtc.Should().NotBeNull();
        snapshot.Security.AuthSuccesses.Should().BeGreaterThanOrEqualTo(1);
    }
}

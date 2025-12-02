using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class TestTokenBuilderTests
{
    [Fact]
    public void Build_CreatesSignedToken_WithClaims()
    {
        var secret = "unit-test-secret-value-32bytes!!";
        var tokenString = TestTokenBuilder.Create()
            .WithIssuer("https://test.local")
            .WithAudience("api://test")
            .WithSecret(secret)
            .WithClaim("sub", "user-123")
            .WithClaim("email", "test@example.com")
            .Build();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(tokenString).Should().BeTrue();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://test.local",
            ValidateAudience = true,
            ValidAudience = "api://test",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateLifetime = false
        };

        var principal = handler.ValidateToken(tokenString, validationParameters, out _);
        principal.FindFirst("sub")?.Value.Should().Be("user-123");
        principal.FindFirst("email")?.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void CreateFromConfig_BindsIssuerAudienceSecretAndLifetime()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Issuer"] = "https://auth.local",
                ["Audiences:0"] = "api://primus-livedemo",
                ["Secret"] = "32+CharacterSecretValueForLocalUse",
                ["TestTokenLifetimeMinutes"] = "120"
            })
            .Build();

        var tokenString = TestTokenBuilder.CreateFromConfig(config)
            .WithClaim("sub", "cfg-user")
            .Build();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);
        token.Issuer.Should().Be("https://auth.local");
        token.Audiences.Should().Contain("api://primus-livedemo");
        token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddHours(2), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void WithExpiry_TimeSpan_SetsRelativeExpiryFromUtcNow()
    {
        var secret = "unit-test-secret-value-32bytes!!";
        var tokenString = TestTokenBuilder.Create()
            .WithSecret(secret)
            .WithExpiry(TimeSpan.FromMinutes(5))
            .Build();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);
        token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
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
}

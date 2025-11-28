using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Lightweight helper to generate signed JWTs for local development and automated tests.
/// </summary>
public class TestTokenBuilder
{
    private readonly List<Claim> _claims = new();
    private readonly List<string> _audiences = new();
    private string _issuer = "https://localhost";
    private DateTimeOffset _expiresAt = DateTimeOffset.UtcNow.AddHours(1);
    private string _secret = "local-test-secret-please-change";

    public static TestTokenBuilder Create() => new();

    public TestTokenBuilder WithIssuer(string issuer)
    {
        _issuer = issuer;
        return this;
    }

    public TestTokenBuilder WithAudience(string audience)
    {
        if (!string.IsNullOrWhiteSpace(audience))
        {
            _audiences.Add(audience);
        }
        return this;
    }

    public TestTokenBuilder WithExpiry(DateTimeOffset expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }

    public TestTokenBuilder WithSecret(string secret)
    {
        _secret = secret;
        return this;
    }

    public TestTokenBuilder WithClaim(string type, string value)
    {
        _claims.Add(new Claim(type, value));
        return this;
    }

    public TestTokenBuilder WithClaims(IEnumerable<Claim> claims)
    {
        _claims.AddRange(claims);
        return this;
    }

    /// <summary>
    /// Builds the signed JWT using HMAC-SHA256.
    /// </summary>
    public string Build()
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audiences.FirstOrDefault(),
            claims: _claims,
            notBefore: DateTime.UtcNow,
            expires: _expiresAt.UtcDateTime,
            signingCredentials: creds);

        return handler.WriteToken(token);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LiveDemoApi.Controllers;

public record LocalAuthRequest(string Email, string Password, string? Name);

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("local")]
    public IActionResult Local(LocalAuthRequest request)
    {
        return IssueLocalToken(request, "LocalJwt");
    }

    [HttpPost("auth0")]
    public IActionResult Auth0Fallback()
    {
        _logger.LogInformation("Auth0 button pressed; issuing local demo token because Auth0 is not configured.");
        return IssueLocalToken(new LocalAuthRequest("demo@primus.local", "PrimusDemo123!", "Auth0 Demo User"), "Auth0Fallback");
    }

    [HttpPost("azure")]
    public IActionResult AzureFallback()
    {
        _logger.LogInformation("Azure AD button pressed; issuing local demo token because Azure AD is not configured.");
        return IssueLocalToken(new LocalAuthRequest("demo@primus.local", "PrimusDemo123!", "Azure Demo User"), "AzureFallback");
    }

    private IActionResult IssueLocalToken(LocalAuthRequest request, string provider)
    {
        var demo = _configuration.GetSection("DemoLocalAuth");
        var expectedEmail = demo["Email"] ?? "demo@primus.local";
        var expectedPassword = demo["Password"] ?? "PrimusDemo123!";
        var subject = demo["Subject"] ?? "local-demo-user";
        var name = request.Name ?? demo["Name"] ?? "Primus Demo User";

        if (!string.Equals(request.Email, expectedEmail, StringComparison.OrdinalIgnoreCase) ||
            request.Password != expectedPassword)
        {
            _logger.LogWarning("Local auth failed for {Email}", request.Email);
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var localIssuer = _configuration["PrimusIdentity:Issuers:2:Issuer"] ?? "https://primus.local";
        var audience = _configuration.GetSection("PrimusIdentity:Issuers:2:Audiences").Get<string[]>()?.FirstOrDefault()
                       ?? "api://primus-livedemo";
        var secret = _configuration["PrimusIdentity:Issuers:2:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogError("Local auth secret missing in configuration.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Local auth misconfigured." });
        }

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Email, expectedEmail),
            new("name", name),
            new("provider", provider),
            new("demo", "true")
        };

        var jwt = new JwtSecurityToken(
            issuer: localIssuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials);

        var accessToken = _tokenHandler.WriteToken(jwt);
        _logger.LogInformation("✅ Issued local JWT for {Email} via provider {Provider}", request.Email, provider);
        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            provider
        });
    }
}

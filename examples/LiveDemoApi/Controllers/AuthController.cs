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
            new("provider", "LocalJwt"),
            new("demo", "true")
        };

        var jwt = new JwtSecurityToken(
            issuer: localIssuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials);

        var accessToken = _tokenHandler.WriteToken(jwt);
        _logger.LogInformation("✅ Issued local JWT for {Email}", request.Email);
        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600
        });
    }

    [HttpPost("auth0")]
    public IActionResult Auth0Fallback()
    {
        _logger.LogWarning("Auth0 proxy endpoint called but not configured. Returning guidance.");
        return BadRequest(new { error = "Auth0 proxy not configured in this demo. Use Local JWT login." });
    }

    [HttpPost("azure")]
    public IActionResult AzureFallback()
    {
        _logger.LogWarning("Azure proxy endpoint called but not configured. Returning guidance.");
        return BadRequest(new { error = "Azure CLI token proxy not configured in this demo. Use Local JWT login." });
    }
}

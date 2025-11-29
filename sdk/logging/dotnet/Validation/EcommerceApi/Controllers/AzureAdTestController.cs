using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Identity.Validator;
using System.IdentityModel.Tokens.Jwt;

namespace EcommerceApi.Controllers;

/// <summary>
/// Controller to test Azure AD / Entra ID token validation via PrimusSaaS.Identity.Validator
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AzureAdTestController : ControllerBase
{
    private readonly Logger _logger;

    public AzureAdTestController(Logger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Public endpoint - no authentication required
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public()
    {
        return Ok(new
        {
            message = "✅ Azure AD Test - Public endpoint (no auth required)",
            timestamp = DateTime.UtcNow,
            provider = "Azure AD / Entra ID"
        });
    }

    /// <summary>
    /// Protected endpoint - requires valid Azure AD or Auth0 token
    /// The package's JWT Bearer middleware validates the token automatically
    /// </summary>
    [HttpGet("protected")]
    [Authorize]
    public IActionResult Protected()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        
        // Get key claims
        var issuer = User.Claims.FirstOrDefault(c => c.Type == "iss")?.Value ?? "unknown";
        var appId = User.Claims.FirstOrDefault(c => c.Type == "azp" || c.Type == "appid")?.Value ?? "unknown";
        var audience = User.Claims.FirstOrDefault(c => c.Type == "aud")?.Value ?? "unknown";
        var matched = HttpContext.GetMatchedIssuer();

        _logger.Info("Azure AD protected endpoint accessed", new Dictionary<string, object?>
        {
            ["issuer"] = issuer,
            ["appId"] = appId,
            ["audience"] = audience,
            ["claimsCount"] = claims.Count
        });

        return Ok(new
        {
            message = "✅ Welcome to the Azure AD protected resource!",
            provider = matched?.Provider ?? matched?.Name ?? (issuer.Contains("microsoftonline") ? "Azure AD / Entra ID" : "Auth0"),
            matchedIssuer = matched?.Issuer,
            issuer,
            appId,
            audience,
            claimsCount = claims.Count,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Decode a token without validation (for debugging)
    /// </summary>
    [HttpPost("decode")]
    [AllowAnonymous]
    public IActionResult DecodeToken([FromBody] TokenRequest request)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(request.Token);

            return Ok(new
            {
                message = "Token decoded (NOT validated)",
                header = new
                {
                    jwt.Header.Alg,
                    jwt.Header.Typ,
                    jwt.Header.Kid
                },
                payload = new
                {
                    issuer = jwt.Issuer,
                    audience = jwt.Audiences.FirstOrDefault(),
                    subject = jwt.Subject,
                    issuedAt = jwt.IssuedAt,
                    expires = jwt.ValidTo,
                    appId = jwt.Claims.FirstOrDefault(c => c.Type == "azp" || c.Type == "appid")?.Value,
                    version = jwt.Claims.FirstOrDefault(c => c.Type == "ver")?.Value,
                    claimsCount = jwt.Claims.Count()
                },
                allClaims = jwt.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get Azure AD JWKS information
    /// </summary>
    [HttpGet("jwks")]
    [AllowAnonymous]
    public async Task<IActionResult> GetJwks([FromQuery] string tenantId = "cbd15a9b-cd52-4ccc-916a-00e2edb13043")
    {
        try
        {
            using var client = new HttpClient();
            var jwksUrl = $"https://login.microsoftonline.com/{tenantId}/discovery/v2.0/keys";
            var jwks = await client.GetStringAsync(jwksUrl);
            var keys = System.Text.Json.JsonDocument.Parse(jwks);
            var keyCount = keys.RootElement.GetProperty("keys").GetArrayLength();

            return Ok(new
            {
                message = "Azure AD JWKS retrieved",
                jwksUrl,
                keyCount,
                keys = keys.RootElement
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test endpoint to show multi-provider support
    /// </summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    public IActionResult ListProviders()
    {
        return Ok(new
        {
            message = "PrimusSaaS.Identity.Validator supports multiple identity providers",
            configuredProviders = new[]
            {
                new
                {
                    name = "auth0",
                    type = "Auth0",
                    issuer = "https://dev-ft7bykiq2exe4ua4.us.auth0.com/",
                    audience = "https://saas-api/"
                },
                new
                {
                    name = "azuread",
                    type = "AzureAD",
                    issuer = "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0",
                    audience = "api://d91ce212-625e-4bdb-9b3f-428a831077a4"
                }
            },
            note = "Any valid token from these providers will be accepted on [Authorize] endpoints"
        });
    }

    public class TokenRequest
    {
        public string Token { get; set; } = "";
    }
}

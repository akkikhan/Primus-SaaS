using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Identity.Validator.Services;
using PrimusSaaS.Logging.Core;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EcommerceApi.Controllers;

/// <summary>
/// Controller for testing real Auth0 token validation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Auth0TestController : ControllerBase
{
    private readonly Logger _logger;
    private readonly JwksService _jwksService;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public Auth0TestController(Logger logger, JwksService jwksService)
    {
        _logger = logger;
        _jwksService = jwksService;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Public endpoint - no authentication required
    /// </summary>
    [HttpGet("public")]
    public IActionResult PublicEndpoint()
    {
        _logger.Info("Public endpoint accessed");
        return Ok(new
        {
            message = "This is a public endpoint - no token required",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Decode a token without validation (shows JWT structure)
    /// </summary>
    [HttpPost("decode")]
    public IActionResult DecodeToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new { error = "Token is required" });
        }

        try
        {
            var jwt = _tokenHandler.ReadJwtToken(request.Token);

            return Ok(new
            {
                message = "Token decoded (NOT validated)",
                header = new
                {
                    alg = jwt.Header.Alg,
                    typ = jwt.Header.Typ,
                    kid = jwt.Header.Kid
                },
                payload = new
                {
                    issuer = jwt.Issuer,
                    subject = jwt.Subject,
                    audiences = jwt.Audiences.ToList(),
                    issuedAt = jwt.IssuedAt,
                    expiresAt = jwt.ValidTo,
                    claims = jwt.Claims.Select(c => new { c.Type, c.Value }).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to decode token", message = ex.Message });
        }
    }

    /// <summary>
    /// Validate Auth0 token using JWKS from Auth0
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromBody] TokenRequest request)
    {
        _logger.Info("Token validation requested", new Dictionary<string, object?>
        {
            ["tokenLength"] = request.Token?.Length ?? 0
        });

        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new { error = "Token is required" });
        }

        try
        {
            // Fetch JWKS from Auth0
            var jwksUri = "https://dev-ft7bykiq2exe4ua4.us.auth0.com/.well-known/jwks.json";
            _logger.Info("Fetching JWKS", new Dictionary<string, object?> { ["uri"] = jwksUri });
            
            var jwks = await _jwksService.GetJwksAsync(jwksUri);
            var securityKeys = JwksCache.GetSecurityKeys(jwks);

            _logger.Info("JWKS fetched", new Dictionary<string, object?>
            {
                ["keyCount"] = securityKeys.Count,
                ["keyIds"] = string.Join(", ", securityKeys.Select(k => k.KeyId))
            });

            // Configure validation parameters
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://dev-ft7bykiq2exe4ua4.us.auth0.com/",
                ValidateAudience = true,
                ValidAudience = "https://saas-api/",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = securityKeys,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            // Validate the token
            var principal = _tokenHandler.ValidateToken(request.Token, validationParameters, out var validatedToken);

            var jwt = validatedToken as JwtSecurityToken;

            _logger.Info("Token validated successfully", new Dictionary<string, object?>
            {
                ["issuer"] = jwt?.Issuer,
                ["subject"] = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value,
                ["claimsCount"] = principal.Claims.Count()
            });

            return Ok(new
            {
                isValid = true,
                message = "✅ Auth0 token validated successfully using JWKS!",
                issuer = jwt?.Issuer,
                subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value,
                audience = jwt?.Audiences.FirstOrDefault(),
                expiresAt = jwt?.ValidTo,
                algorithm = jwt?.Header.Alg,
                keyId = jwt?.Header.Kid,
                claims = principal.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                jwksInfo = new
                {
                    uri = jwksUri,
                    keysFound = securityKeys.Count
                }
            });
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.Warn("Token validation failed", new Dictionary<string, object?>
            {
                ["error"] = ex.Message
            });

            return Unauthorized(new
            {
                isValid = false,
                error = ex.Message,
                message = "❌ Token validation failed"
            });
        }
        catch (Exception ex)
        {
            _logger.Error("Token validation exception", new Dictionary<string, object?>
            {
                ["exception"] = ex.Message,
                ["message"] = ex.Message
            });

            return StatusCode(500, new
            {
                error = "Token validation error",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Protected endpoint using ASP.NET Core [Authorize] attribute
    /// </summary>
    [Authorize]
    [HttpGet("protected")]
    public IActionResult ProtectedEndpoint()
    {
        var matched = HttpContext.GetMatchedIssuer();
        _logger.Info("Protected resource accessed via [Authorize]", new Dictionary<string, object?>
        {
            ["user"] = User.Identity?.Name,
            ["isAuthenticated"] = User.Identity?.IsAuthenticated,
            ["provider"] = matched?.Provider ?? matched?.Name ?? "unknown",
            ["issuer"] = matched?.Issuer
        });

        return Ok(new
        {
            message = "✅ Welcome to the protected resource!",
            authenticatedVia = "JWT Bearer middleware",
            isAuthenticated = User.Identity?.IsAuthenticated,
            provider = matched?.Provider ?? matched?.Name ?? "unknown",
            issuer = matched?.Issuer,
            claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }

    /// <summary>
    /// Test JWKS fetching and caching
    /// </summary>
    [HttpGet("jwks")]
    public async Task<IActionResult> GetJwks()
    {
        var jwksUri = "https://dev-ft7bykiq2exe4ua4.us.auth0.com/.well-known/jwks.json";

        _logger.Info("Testing JWKS fetch", new Dictionary<string, object?> { ["uri"] = jwksUri });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var jwks1 = await _jwksService.GetJwksAsync(jwksUri);
        var firstFetchMs = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        var jwks2 = await _jwksService.GetJwksAsync(jwksUri);
        var cachedFetchMs = stopwatch.ElapsedMilliseconds;

        return Ok(new
        {
            message = "JWKS fetch test complete",
            jwksUri = jwksUri,
            keys = jwks1.Keys.Select(k => new
            {
                keyId = k.KeyId,
                keyType = k.KeyType,
                algorithm = k.Algorithm,
                use = k.Use
            }).ToList(),
            performance = new
            {
                firstFetchMs = firstFetchMs,
                cachedFetchMs = cachedFetchMs,
                isCached = cachedFetchMs < firstFetchMs / 2 ? "Yes (likely)" : "Maybe not"
            }
        });
    }

    /// <summary>
    /// Diagnose JWT Bearer authentication issues
    /// </summary>
    [HttpGet("diagnose")]
    public async Task<IActionResult> DiagnoseAuth([FromServices] Microsoft.AspNetCore.Authentication.IAuthenticationService authService)
    {
        // Try to authenticate using the JWT Bearer scheme
        var result = await authService.AuthenticateAsync(HttpContext, "Bearer");
        
        if (result.Succeeded)
        {
            return Ok(new
            {
                success = true,
                message = "Authentication succeeded!",
                user = result.Principal?.Identity?.Name,
                claims = result.Principal?.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }
        else
        {
            return Ok(new
            {
                success = false,
                message = "Authentication failed",
                error = result.Failure?.Message,
                innerError = result.Failure?.InnerException?.Message,
                fullError = result.Failure?.ToString()
            });
        }
    }
}

public class TokenRequest
{
    public string? Token { get; set; }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Portal.Api.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly PortalDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IConfigurationManager<OpenIdConnectConfiguration>? _azureConfigManager;
    private readonly string? _azureTenantId;
    private readonly string? _azureClientId;
    private readonly string? _azureAudience;

    public AuthController(PortalDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;

        _azureTenantId = _configuration["AzureAd:TenantId"];
        _azureClientId = _configuration["AzureAd:ClientId"];
        var configuredAudience = _configuration["AzureAd:Audience"];
        _azureAudience = string.IsNullOrWhiteSpace(configuredAudience) ? _azureClientId : configuredAudience;

        if (!string.IsNullOrWhiteSpace(_azureTenantId) && !string.IsNullOrWhiteSpace(_azureClientId))
        {
            var authority = $"https://login.microsoftonline.com/{_azureTenantId}/v2.0";
            var metadataAddress = $"{authority}/.well-known/openid-configuration";
            _azureConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever());
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Find user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Verify password (using BCrypt in production)
        // For now, simple comparison - TODO: Implement BCrypt
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Generate JWT token
        var token = GenerateJwtToken(user.Id, user.Email, user.Role.ToString());

        return Ok(new LoginResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role.ToString()
        });
    }

    [HttpPost("azure")]
    public async Task<ActionResult<LoginResponse>> AzureLogin([FromBody] AzureLoginRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("[Azure Login] Endpoint called");
        Console.WriteLine($"[Azure Login] IdToken present: {!string.IsNullOrWhiteSpace(request?.IdToken)}");
        
        if (_azureConfigManager == null || string.IsNullOrWhiteSpace(_azureTenantId) || string.IsNullOrWhiteSpace(_azureClientId))
        {
            Console.WriteLine("[Azure Login] Azure AD not configured");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Azure AD login is not configured." });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
        {
            Console.WriteLine("[Azure Login] IdToken missing");
            return BadRequest(new { message = "Azure AD ID token is required." });
        }

        try
        {
            Console.WriteLine("[Azure Login] Validating Azure AD token...");
            var configuration = await _azureConfigManager.GetConfigurationAsync(cancellationToken);

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[]
                {
                    $"https://login.microsoftonline.com/{_azureTenantId}/v2.0",
                    $"https://sts.windows.net/{_azureTenantId}/"
                },
                ValidateAudience = true,
                ValidAudience = _azureAudience ?? _azureClientId,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                IssuerSigningKeys = configuration.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(request.IdToken, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { message = "Invalid Azure AD token algorithm." });
            }

            Console.WriteLine("[Azure Login] Token validated successfully");
            
            // Log all claims for debugging
            var claims = principal.Claims.Select(c => $"{c.Type}={c.Value}");
            Console.WriteLine($"[Azure Login] All claims: {string.Join(", ", claims)}");
            
            var tokenTenant = principal.FindFirst("tid")?.Value;
            Console.WriteLine($"[Azure Login] Token tenant (tid): {tokenTenant}");
            Console.WriteLine($"[Azure Login] Expected tenant: {_azureTenantId}");
            
            if (!string.IsNullOrWhiteSpace(tokenTenant) && !string.Equals(tokenTenant, _azureTenantId, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[Azure Login] Tenant mismatch");
                return Unauthorized(new { message = "Token tenant does not match configured tenant." });
            }

            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                        ?? principal.FindFirst("preferred_username")?.Value
                        ?? principal.FindFirst(ClaimTypes.Upn)?.Value;

            Console.WriteLine($"[Azure Login] Extracted email: {email}");
            
            if (string.IsNullOrWhiteSpace(email))
            {
                Console.WriteLine("[Azure Login] Email missing from token");
                return Unauthorized(new { message = "Azure AD token is missing an email claim." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user == null)
            {
                Console.WriteLine($"[Azure Login] User not found in database: {email}");
                return Unauthorized(new { message = "No matching portal user found for Azure AD account." });
            }

            Console.WriteLine($"[Azure Login] User found: {user.Email} (Role: {user.Role})");
            var token = GenerateJwtToken(user.Id, user.Email, user.Role.ToString());
            Console.WriteLine("[Azure Login] Session token generated successfully");

            return Ok(new LoginResponse
            {
                Token = token,
                Email = user.Email,
                Role = user.Role.ToString()
            });
        }
        catch (SecurityTokenException ex)
        {
            Console.WriteLine($"[Azure Login] SecurityTokenException: {ex.Message}");
            return Unauthorized(new { message = $"Invalid Azure AD token: {ex.Message}" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Azure Login] Exception: {ex.Message}");
            Console.WriteLine($"[Azure Login] Stack trace: {ex.StackTrace}");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Azure AD login failed: {ex.Message}" });
        }
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    private string GenerateJwtToken(int userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"])),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record LoginRequest(string Email, string Password);
public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public record AzureLoginRequest(string IdToken);

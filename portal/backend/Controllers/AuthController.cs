using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;

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
    private readonly JwtOptions _jwtOptions;

    public AuthController(PortalDbContext context, IConfiguration configuration, IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _configuration = configuration;
        _jwtOptions = jwtOptions.Value;

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
        if (_azureConfigManager == null || string.IsNullOrWhiteSpace(_azureTenantId) || string.IsNullOrWhiteSpace(_azureClientId))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Azure AD login is not configured." });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
        {
            return BadRequest(new { message = "Azure AD ID token is required." });
        }

        try
        {
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

            var tokenTenant = principal.FindFirst("tid")?.Value;
            
            if (!string.IsNullOrWhiteSpace(tokenTenant) && !string.Equals(tokenTenant, _azureTenantId, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { message = "Token tenant does not match configured tenant." });
            }

            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                        ?? principal.FindFirst("preferred_username")?.Value
                        ?? principal.FindFirst(ClaimTypes.Upn)?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized(new { message = "Azure AD token is missing an email claim." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user == null)
            {
                return Unauthorized(new { message = "No matching portal user found for Azure AD account." });
            }

            var token = GenerateJwtToken(user.Id, user.Email, user.Role.ToString());

            return Ok(new LoginResponse
            {
                Token = token,
                Email = user.Email,
                Role = user.Role.ToString()
            });
        }
        catch (SecurityTokenException ex)
        {
            return Unauthorized(new { message = $"Invalid Azure AD token: {ex.Message}" });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Azure AD login failed. Please try again." });
        }
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    private string GenerateJwtToken(int userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.EffectiveKey!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtOptions.ExpiryInMinutes)),
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

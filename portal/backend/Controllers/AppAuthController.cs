using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/auth/app")]
public class AppAuthController : ControllerBase
{
    private readonly PortalDbContext _context;
    private readonly IConfiguration _configuration;

    public AppAuthController(PortalDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AppUserResponse>> Register([FromBody] AppUserRegisterRequest request)
    {
        // 1. Find the Application
        var app = await _context.Applications.FirstOrDefaultAsync(a => a.PrimusClientId == request.ClientId);
        if (app == null)
        {
            return NotFound(new { message = "Application not found" });
        }

        // 2. Check if user exists
        if (await _context.AppUsers.AnyAsync(u => u.ApplicationId == app.Id && u.Username == request.Username))
        {
            return BadRequest(new { message = "Username already exists for this application" });
        }

        // 3. Create User
        var user = new AppUser
        {
            ApplicationId = app.Id,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new AppUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AppLoginResponse>> Login([FromBody] AppLoginRequest request)
    {
        // 1. Find the Application
        var app = await _context.Applications.FirstOrDefaultAsync(a => a.PrimusClientId == request.ClientId);
        if (app == null)
        {
            return NotFound(new { message = "Application not found" });
        }

        // 2. Find the User
        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.ApplicationId == app.Id && u.Username == request.Username);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // 3. Verify Password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // 4. Generate Token using App's Signing Key
        if (string.IsNullOrEmpty(app.JwtSigningKey))
        {
            return StatusCode(500, new { message = "Application JWT signing key is not configured" });
        }

        var token = GenerateJwtToken(user, app);

        return Ok(new AppLoginResponse
        {
            Token = token,
            Username = user.Username,
            ExpiresIn = 3600 // 1 hour
        });
    }

    private string GenerateJwtToken(AppUser user, Application app)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(app.JwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("app_id", app.PrimusClientId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "https://primus-portal.com", // Should match SDK config
            audience: app.PrimusClientId,        // Should match SDK config
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record AppUserRegisterRequest(string ClientId, string Username, string Email, string Password);
public record AppUserResponse
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public record AppLoginRequest(string ClientId, string Username, string Password);
public record AppLoginResponse
{
    public string Token { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
}

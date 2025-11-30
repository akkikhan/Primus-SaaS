using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveDemoApi.Controllers;

[ApiController]
[Route("whoami")]
public class IdentityController : ControllerBase
{
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(ILogger<IdentityController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        var user = HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Unauthorized request to /whoami");
            return Unauthorized();
        }

        var claims = user.Claims.Select(c => new { c.Type, c.Value }).ToArray();
        var issuer = user.FindFirst("iss")?.Value ?? "unknown";
        var audience = user.FindFirst("aud")?.Value ?? "unknown";
        var subject = user.FindFirst("sub")?.Value
                   ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                   ?? "unknown";
        var email = user.FindFirst("email")?.Value
                 ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        var name = user.FindFirst("name")?.Value
                ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

        _logger.LogInformation("✅ /whoami: issuer {Issuer}", issuer);
        return Ok(new
        {
            authenticated = true,
            identity = new { subject, email, name, issuer, audience },
            claims,
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
}

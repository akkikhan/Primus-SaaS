# Primus SaaS Identity Validator - .NET SDK

Official .NET SDK for validating JWT tokens issued by the Primus SaaS Portal. This package provides middleware and extensions for ASP.NET Core applications to easily authenticate users.

## Installation

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

Or via NuGet Package Manager:

```
Install-Package PrimusSaaS.Identity.Validator
```

## Quick Start

### 1. Configure in `Program.cs` or `Startup.cs`

```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Identity validation
builder.Services.AddPrimusIdentity(options =>
{
    options.PortalUrl = "https://portal.primus-saas.com";
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.JwtSecret = "your-jwt-secret-key";
    
    // Optional: Configure additional settings
    options.ValidateLifetime = true;
    options.RequireHttpsMetadata = true; // Set false for development
    options.ClockSkew = TimeSpan.FromMinutes(5);
});

// Add authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Enable authentication & authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

### 2. Protect Your API Endpoints

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator;

[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    [HttpGet]
    [Authorize] // Requires valid Primus SaaS JWT token
    public IActionResult GetSecureData()
    {
        // Get the authenticated Primus user
        var primusUser = HttpContext.GetPrimusUser();
        
        return Ok(new
        {
            message = "Secure data accessed successfully",
            user = new
            {
                userId = primusUser?.UserId,
                email = primusUser?.Email,
                name = primusUser?.Name,
                roles = primusUser?.Roles
            }
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")] // Requires Admin role
    public IActionResult GetAdminData()
    {
        return Ok(new { message = "Admin-only data" });
    }
}
```

### 3. Access User Information

```csharp
// In any controller or middleware
var primusUser = HttpContext.GetPrimusUser();

if (primusUser != null)
{
    Console.WriteLine($"User ID: {primusUser.UserId}");
    Console.WriteLine($"Email: {primusUser.Email}");
    Console.WriteLine($"Name: {primusUser.Name}");
    Console.WriteLine($"Roles: {string.Join(", ", primusUser.Roles)}");
    
    // Access additional claims
    foreach (var claim in primusUser.AdditionalClaims)
    {
        Console.WriteLine($"{claim.Key}: {claim.Value}");
    }
}
```

## Configuration Options

| Option | Required | Description | Default |
|--------|----------|-------------|---------|
| `PortalUrl` | Yes | Base URL of Primus SaaS Portal | - |
| `ClientId` | Yes | Your application's Client ID | - |
| `ClientSecret` | Yes | Your application's Client Secret | - |
| `JwtSecret` | Yes | JWT secret key from portal | - |
| `Issuer` | No | Expected token issuer | PortalUrl |
| `Audience` | No | Expected token audience | ClientId |
| `ValidateLifetime` | No | Validate token expiration | `true` |
| `RequireHttpsMetadata` | No | Require HTTPS for metadata | `true` |
| `ClockSkew` | No | Allowed time difference | 5 minutes |

## Configuration from appsettings.json

```json
{
  "PrimusIdentity": {
    "PortalUrl": "https://portal.primus-saas.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "JwtSecret": "your-jwt-secret-key",
    "RequireHttpsMetadata": false
  }
}
```

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});
```

## Client Usage Example

To call your protected API from a client application:

```csharp
using System.Net.Http.Headers;

var httpClient = new HttpClient();
var jwtToken = "your-jwt-token-from-primus-portal";

// Add token to Authorization header
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", jwtToken);

var response = await httpClient.GetAsync("https://your-api.com/api/secure");
var data = await response.Content.ReadAsStringAsync();
```

## Development Tips

### Disable HTTPS Requirement for Local Development

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.PortalUrl = "http://localhost:5000";
    options.RequireHttpsMetadata = false; // Allow HTTP in development
    // ... other options
});
```

### Enable Detailed Logging

The SDK automatically logs authentication events to the console. For more detailed logging, enable ASP.NET Core logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication": "Debug"
    }
  }
}
```

## Requirements

- .NET 7.0 or later
- ASP.NET Core 7.0 or later

## Support

For issues, questions, or contributions, visit:
- GitHub: https://github.com/akkikhan/Primus-SaaS
- Documentation: https://portal.primus-saas.com/docs

## License

MIT License - see LICENSE file for details

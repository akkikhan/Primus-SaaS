# Primus SaaS Identity Validator - .NET SDK

Official .NET SDK for validating JWT/OIDC tokens from your configured identity providers (Azure AD, LocalAuth, or any JWT issuer). The package is library-only: no Primus-hosted login, no Primus-issued tokens, no outbound calls to Primus.

## Installation

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

Or via NuGet Package Manager:

```powershell
Install-Package PrimusSaaS.Identity.Validator
```

## Quick Start

### 1. Configure in Program.cs or Startup.cs

```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Identity validation (multi-issuer)
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new()
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.Oidc,
            Issuer = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
            Authority = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
            Audiences = new List<string> { "api://your-api-id" }
        },
        new IssuerConfig
        {
            Name = "LocalAuth",
            Type = IssuerType.Jwt,
            Issuer = "https://auth.yourcompany.com",
            Secret = "your-local-secret",
            Audiences = new List<string> { "api://your-api-id" }
        }
    };

    options.ValidateLifetime = true;
    options.RequireHttpsMetadata = true; // Set false for local dev only
    options.ClockSkew = TimeSpan.FromMinutes(5);

    // Optional: map claims to tenant context
    options.TenantResolver = claims => new TenantContext
    {
        TenantId = claims.Get("tid") ?? "default",
        Roles = claims.Get<List<string>>("roles") ?? new List<string>()
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

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
    [Authorize] // Requires valid token from a configured issuer
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
    [Authorize(Roles = "Admin")] // Requires Admin role from your IdP
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
| Issuers | Yes | List of issuer configs (Oidc or Jwt) | - |
| ValidateLifetime | No | Validate token expiration | true |
| RequireHttpsMetadata | No | Require HTTPS for metadata | true |
| ClockSkew | No | Allowed time difference | 5 minutes |
| JwksCacheTtl | No | JWKS cache TTL (OIDC) | 24 hours |
| TenantResolver | No | Map claims to TenantContext | null |

### IssuerConfig

| Field | Required | Description |
|-------|----------|-------------|
| Name | Yes | Friendly name (e.g., AzureAD, LocalAuth) |
| Type | Yes | Oidc or Jwt |
| Issuer | Yes | Expected iss value to route tokens |
| Authority | OIDC only | Authority URL for discovery/JWKS |
| JwksUrl | JWT optional | JWKS endpoint (if not using Secret) |
| Secret | JWT optional | Symmetric key for HMAC tokens |
| Audiences | Yes | Allowed audience values |

## Configuration from appsettings.json

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "Oidc",
        "Issuer": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Audiences": [ "api://your-api-id" ]
      },
      {
        "Name": "LocalAuth",
        "Type": "Jwt",
        "Issuer": "https://auth.yourcompany.com",
        "Secret": "your-local-secret",
        "Audiences": [ "api://your-api-id" ]
      }
    ],
    "RequireHttpsMetadata": false
  }
}
```

## Generating Tokens for Local JWT Issuer

> **Important:** The Secret, Issuer, and Audience values used when generating tokens MUST EXACTLY MATCH your validator configuration.

### Quick Example

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public string GenerateLocalJwtToken(string userId, string email, string name)
{
    // Critical: Load from same configuration source
    var secret = _config["PrimusIdentity:Issuers:1:Secret"];
    var issuer = _config["PrimusIdentity:Issuers:1:Issuer"];
    var audience = _config["PrimusIdentity:Issuers:1:Audiences:0"];
    
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(secret);
    
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim("sub", userId),
            new Claim("email", email),
            new Claim("name", name)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = issuer,
        Audience = audience,
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature
        )
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}
```

For complete token generation examples, see [TOKEN_GENERATION_GUIDE.md](./TOKEN_GENERATION_GUIDE.md)

## Troubleshooting

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| Invalid signature | Secret key mismatch | Ensure token generation and validation use the same secret |
| Untrusted issuer | Issuer format incorrect | Use full URL format (e.g., https://localhost:5265) not name |
| Invalid audience | Audience mismatch | Use API identifier format (e.g., api://your-app-id) |
| Token expired | Token past expiration | Generate new token or increase ClockSkew |

For detailed troubleshooting, see [ERROR_REFERENCE.md](./ERROR_REFERENCE.md)

## Production Deployment

> **Caution:** Never commit secrets to source control! Use Azure Key Vault or environment variables.

### Quick Checklist

- [ ] Secrets stored in Azure Key Vault
- [ ] RequireHttpsMetadata: true in production
- [ ] HTTPS redirection enabled
- [ ] CORS configured for production domains
- [ ] Logging and monitoring configured

For complete deployment guide, see [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md)

## Client Usage Example

To call your protected API from a client application:

```csharp
using System.Net.Http.Headers;


var httpClient = new HttpClient();
var jwtToken = "your-jwt-token-here"; // From your auth system or token generator


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

## Known Issues

### Namespace Conflict with PrimusSaaS.Logging

If you are using both `PrimusSaaS.Identity.Validator` and `PrimusSaaS.Logging`, you may encounter an ambiguous reference error for `UsePrimusLogging()`.

**Solution:** Use the fully qualified name or an alias.

```csharp
using PrimusLogging = PrimusSaaS.Logging.Extensions;

// ...

PrimusLogging.LoggingExtensions.UsePrimusLogging(app);
```

## Documentation

- [TOKEN_GENERATION_GUIDE.md](./TOKEN_GENERATION_GUIDE.md) - Complete guide to generating JWT tokens
- [LOCAL_DEVELOPMENT_GUIDE.md](./LOCAL_DEVELOPMENT_GUIDE.md) - Setup guide for offline/local development
- [INTEGRATION_PATTERNS.md](./INTEGRATION_PATTERNS.md) - Controller, Service, and Middleware examples
- [TENANT_RESOLVER_GUIDE.md](./TENANT_RESOLVER_GUIDE.md) - Multi-tenant context resolution guide
- [PRIMUS_USER_REFERENCE.md](./PRIMUS_USER_REFERENCE.md) - PrimusUser object properties and mapping
- [ERROR_HANDLING_GUIDE.md](./ERROR_HANDLING_GUIDE.md) - Handling exceptions and customizing responses
- [ERROR_REFERENCE.md](./ERROR_REFERENCE.md) - Troubleshooting validation errors
- [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md) - Production deployment best practices
- [SECRET_MANAGEMENT.md](./SECRET_MANAGEMENT.md) - Securely managing secrets (Key Vault, User Secrets)
- [TESTING_GUIDE.md](./TESTING_GUIDE.md) - Testing guide with Postman & Integration Tests
- [CLAIMS_MAPPING.md](./CLAIMS_MAPPING.md) - Reference for required and optional claims
- [ANGULAR_INTEGRATION.md](./ANGULAR_INTEGRATION.md) - Integration guide for Angular applications

## Support

For issues, questions, or contributions, visit:
- GitHub: https://github.com/primus-saas/identity-validator
- Documentation: https://docs.primus-saas.com

## License

MIT License - see LICENSE file for details

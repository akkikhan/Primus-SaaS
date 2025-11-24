# Local Development Guide

## Overview

PrimusSaaS Identity Validator supports a **Local Development Mode** that allows you to work offline without connecting to Azure AD or any external identity provider. This is achieved using the `Jwt` issuer type with a shared secret.

---

## 1. Configuration

Add a `LocalAuth` issuer to your `appsettings.Development.json`. This configuration will only be active in the Development environment.

```json
// appsettings.Development.json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": false,
    "Issuers": [
      {
        "Name": "LocalAuth",
        "Type": "Jwt",
        "Issuer": "https://localhost:5001",
        "Secret": "dev-secret-key-must-be-at-least-32-chars-long",
        "Audiences": [ "api://local-dev" ]
      }
    ]
  }
}
```

> **Note:** For security, you can use User Secrets for the `Secret` value, but for local development convenience, a hardcoded "dev secret" in `appsettings.Development.json` is often acceptable if the file is not deployed to production.

---

## 2. Startup Setup

Ensure your `Program.cs` loads the configuration correctly. The standard setup works automatically:

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    // Load from configuration
    var section = builder.Configuration.GetSection("PrimusIdentity");
    // ... bind options ...
    
    // OR manual setup with environment check
    if (builder.Environment.IsDevelopment())
    {
        options.RequireHttpsMetadata = false;
        options.Issuers.Add(new IssuerConfig
        {
            Name = "LocalAuth",
            Type = IssuerType.Jwt,
            Issuer = "https://localhost:5001",
            Secret = "dev-secret-key-must-be-at-least-32-chars-long",
            Audiences = new List<string> { "api://local-dev" }
        });
    }
});
```

---

## 3. Generating Dev Tokens

You don't need a full auth server. You can generate valid tokens using a simple helper method or a console app.

### Helper Method (Add to a Test Controller)

```csharp
[ApiController]
[Route("api/dev")]
public class DevAuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public DevAuthController(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    [HttpPost("token")]
    public IActionResult GetToken([FromBody] LoginRequest request)
    {
        // ONLY allow in Development
        if (!_env.IsDevelopment()) return NotFound();

        var secret = "dev-secret-key-must-be-at-least-32-chars-long"; // Match config
        var issuer = "https://localhost:5001";
        var audience = "api://local-dev";

        var claims = new List<Claim>
        {
            new Claim("sub", request.UserId ?? "dev-user"),
            new Claim("email", request.Email ?? "dev@local.test"),
            new Claim("name", "Developer User"),
            new Claim("tid", "tenant-1"), // Simulate tenant
            new Claim("roles", "Admin")   // Simulate roles
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.Now.AddDays(1), signingCredentials: creds);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}

public class LoginRequest { public string UserId { get; set; } public string Email { get; set; } }
```

Now you can POST to `/api/dev/token` to get a valid JWT for testing your secured endpoints.

---

## 4. Testing with Postman

1. **Generate Token**: Call `POST /api/dev/token`
2. **Copy Token**: Copy the `token` string from the response.
3. **Configure Auth**: In your secure request tab, select "Bearer Token" and paste the token.
4. **Send Request**: Your API will validate the token using the local secret.

---

## Summary

| Feature | Production (Azure AD) | Local Dev (Jwt) |
|---------|-----------------------|-----------------|
| **Issuer Type** | `Oidc` | `Jwt` |
| **Validation** | Public Keys (JWKS) | Shared Secret (HMAC) |
| **Network** | Requires Internet | Offline / Localhost |
| **Setup** | App Registration required | No external setup |

By using this pattern, you can develop your application logic completely offline, and switch to Azure AD in production simply by changing the configuration.

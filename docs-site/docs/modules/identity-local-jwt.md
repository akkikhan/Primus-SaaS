---
id: identity-local-jwt
title: Identity Validator - Local JWT (Development)
sidebar_position: 4
description: Local JWT validation for development and testing without external identity providers.
---

# Local JWT Guide (Development & Testing)

Use locally-signed JWTs for development and testing without needing Auth0 or Azure AD. Perfect for unit tests, integration tests, and local development.

---

## When to Use Local JWT

- Local development and demos (no external IdP needed)
- Unit/integration tests
- CI smoke tests
*Not for production.*

---

## Step 1: Install Package

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

---

## Step 2: Generate a Signing Key

You need a secret key to sign and validate tokens. Generate one:

### Option A: Random String (Simple)

```csharp
// Generate a 256-bit key
var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
Console.WriteLine(key);
// Example: "K7gNU3sdo+OL0wNhqoVWhr3g6s1xYv72ol/pe/Unols="
```

### Option B: Using OpenSSL

```bash
openssl rand -base64 32
# Example: K7gNU3sdo+OL0wNhqoVWhr3g6s1xYv72ol/pe/Unols=
```

### Option C: Using PowerShell

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

?? **Key must be at least 32 characters (256 bits) for HMAC-SHA256**

---

## Step 3: Configure appsettings.json

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "LocalDev",
        "Type": "Jwt",
        "Secret": "your-256-bit-secret-key-at-least-32-chars-long!!",
        "Issuer": "https://local-dev-issuer",
        "Audiences": [ "local-dev-api" ],
        "ValidateLifetime": true,
        "ClockSkewSeconds": 300
      }
    ],
    "EnableDetailedErrors": true
  }
}
```

### Configuration Reference

| Property | Required | Description |
|----------|----------|-------------|
| `Name` | Yes | Friendly name for logging |
| `Type` | Yes | Must be `"Jwt"` |
| `Secret` | Yes | HMAC secret (min 32 chars) |
| `Issuer` | Yes | Token issuer claim value |
| `Audiences` | Yes | Array of token audience values |
| `ValidateLifetime` | No | Validate expiration (default: true) |
| `ClockSkewSeconds` | No | Allowed clock skew (default: 300) |

---

## Step 4: Complete Program.cs

```csharp
using PrimusSaaS.Identity.Validator;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(opts => 
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts));

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

---

## Step 5: Generate Test Tokens

### Option A: `TestTokenBuilder` (keeps secrets aligned with config)

```csharp
using PrimusSaaS.Identity.Validator;

// Pull issuer/audience/secret straight from PrimusIdentity:Issuers:LocalDev
var token = TestTokenBuilder
    .CreateFromConfig(builder.Configuration.GetSection("PrimusIdentity:Issuers:LocalDev"))
    .WithExpiry(TimeSpan.FromHours(8)) // optional override (default is 1 hour)
    .Build();
```

> If the secret used here does not match `PrimusIdentity:Issuers:<Name>:Secret`, validation will fail with a 401. Keep them in sync to avoid confusing 500s during local testing.

### Option B: Manual Token Generator

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class TestTokenGenerator
{
    private const string SecretKey = "your-256-bit-secret-key-at-least-32-chars-long!!";
    private const string Issuer = "https://local-dev-issuer";
    private const string Audience = "local-dev-api";

    public static string GenerateToken(
        string userId = "test-user-123",
        string email = "test@example.com",
        int expiresInMinutes = 60)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("name", "Test User")
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateExpiredToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[] { new Claim("sub", "expired-user") },
            expires: DateTime.UtcNow.AddMinutes(-10),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Option C: Add Token Endpoint to Your API

```csharp
// Add this endpoint for development only!
if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/token", (TokenRequest request) =>
    {
        var token = TestTokenGenerator.GenerateToken(
            userId: request.UserId ?? "dev-user",
            email: request.Email ?? "dev@example.com",
            expiresInMinutes: request.ExpiresInMinutes ?? 60
        );
        
        return new { 
            access_token = token,
            token_type = "Bearer",
            expires_in = (request.ExpiresInMinutes ?? 60) * 60
        };
    });
}

record TokenRequest(
    string UserId, 
    string Email, 
    int? ExpiresInMinutes);
```

---

## Step 6: Test Your API

```bash
# Generate a token using curl (if you added the /dev/token endpoint)
TOKEN=$(curl -s -X POST http://localhost:5000/dev/token \
  -H "Content-Type: application/json" \
  -d '{"userId": "user-1", "email": "user@test.com"}' \
  | jq -r '.access_token')

# Test protected endpoint
curl http://localhost:5000/api/secure \
  -H "Authorization: Bearer $TOKEN"
```

---

## Working Example: Full Test API

```csharp
using PrimusSaaS.Identity.Validator;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(opts => 
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Health check
app.MapGet("/", () => new { status = "healthy", auth = "Local JWT" });

// Token generator (development only!)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/token", (string? user, string? email) =>
    {
        var config = builder.Configuration.GetSection("PrimusIdentity:Issuers:0");
        var signingKey = config["Secret"]!;
        var issuer = config["Issuer"]!;
        var audience = config.GetSection("Audiences").Get<string[]>()?.FirstOrDefault() ?? "";
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user ?? "dev-user"),
            new Claim(JwtRegisteredClaimNames.Email, email ?? "dev@example.com"),
            new Claim("name", "Dev User")
        };
        
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );
        
        return new { 
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expires = token.ValidTo
        };
    });
}

// Protected endpoint
app.MapGet("/me", [Authorize] (HttpContext ctx) =>
{
    return new {
        userId = ctx.User.FindFirst("sub")?.Value,
        email = ctx.User.FindFirst("email")?.Value,
        name = ctx.User.FindFirst("name")?.Value
    };
});

app.Run();
```

### appsettings.Development.json

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "LocalDev",
        "Type": "Jwt",
        "Secret": "your-256-bit-secret-key-at-least-32-chars-long!!",
        "Issuer": "https://local-dev-issuer",
        "Audiences": [ "local-dev-api" ]
      }
    ],
    "EnableDetailedErrors": true
  }
}
```

---

## Using Local JWT in Unit Tests

```csharp
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsOk()
    {
        var token = TestTokenGenerator.GenerateToken(
            userId: "test-user",
            email: "test@example.com"
        );
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_Returns401()
    {
        var token = TestTokenGenerator.GenerateExpiredToken();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

---

## Troubleshooting

### Error: "IDX10603: Key size must be greater than 256 bits"

**Cause:** Signing key too short.

**Solution:** Use a key with at least 32 characters:
```json
"Secret": "at-least-32-characters-long-secret-key-here!!"
```

### Error: "IDX10223: Lifetime validation failed"

**Cause:** Token expired.

**Solution:** Generate a new token or disable lifetime validation for testing:
```json
"ValidateLifetime": false
```

### Error: "IDX10208: Unable to validate audience"

**Cause:** Audience mismatch.

**Solution:** Ensure token `aud` claim matches configured `Audience`.

### Debug: Decode token

```csharp
var handler = new JwtSecurityTokenHandler();
var jwt = handler.ReadJwtToken(token);
Console.WriteLine($"Issuer: {jwt.Issuer}");
Console.WriteLine($"Audience: {string.Join(", ", jwt.Audiences)}");
Console.WriteLine($"Expires: {jwt.ValidTo}");
foreach (var claim in jwt.Claims)
{
    Console.WriteLine($"  {claim.Type}: {claim.Value}");
}
```

---

## Security Reminder

?? **Local JWT is for development/testing only!**

- Never use the same signing key in production
- Never commit signing keys to source control
- Use environment variables or user secrets for keys:
  ```bash
  dotnet user-secrets set "PrimusIdentity:Issuers:0:Secret" "your-secret"
  ```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Use Auth0 in production | [Auth0 Guide →](/docs/modules/identity-auth0) |
| Use Azure AD in production | [Azure AD Guide →](/docs/modules/identity-azure-ad) |
| Combine with external providers | [Multi-Issuer Setup →](/docs/modules/identity-multi-issuer) |
| Swagger integration & diagnostics | [Advanced Features →](/docs/modules/identity-advanced) |

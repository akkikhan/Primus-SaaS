---
id: identity-auth0
title: Identity Validator - Auth0 Integration
sidebar_position: 2
description: Complete Auth0 JWT validation setup with full examples.
---

# Auth0 Integration Guide

> See also: [Identity Validator Overview](./identity-validator) for package versions, OIDC/JWT options, and Node.js + .NET entrypoints.

Complete guide to integrating Auth0 authentication with your .NET API using Primus Identity Validator.

---

## Prerequisites

- Auth0 account with an API configured
- .NET 6.0+ project
- Your Auth0 Domain and API Identifier

---

## Step 1: Install Package

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

---

## Step 2: Get Your Auth0 Credentials

From the [Auth0 Dashboard](https://manage.auth0.com/):

1. Go to **Applications -> APIs**
2. Select your API (or create one)
3. Note down:
   - **Domain**: `your-tenant.auth0.com`
   - **API Identifier** (Audience): `https://your-api`

---

## Step 3: Configure appsettings.json

```json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": true,
    "ValidateLifetime": true,
    "Issuers": [
      {
        "Name": "Auth0-Production",
        "Type": "Auth0",
        "Authority": "https://YOUR-TENANT.auth0.com/",
        "Issuer": "https://YOUR-TENANT.auth0.com/",
        "Audiences": ["https://your-api-identifier"]
      }
    ],
    "Diagnostics": {
      "EnableDetailedErrors": true,
      "IncludeTokenHintsInChallenges": true,
      "IncludeDebugHeaders": true,
      "LogTokenRejectionReasons": true,
      "MaxRecentFailures": 50,
      "AutoDetectDevelopment": true
    }
  }
}
```

### Configuration Reference

| Property                | Required | Description                                   |
| ----------------------- | -------- | --------------------------------------------- |
| `Name`                  | Yes      | Friendly name for logging                     |
| `Type`                  | Yes      | Must be `"Auth0"`                             |
| `Authority`             | Yes      | Your Auth0 domain with trailing slash         |
| `Issuer`                | Yes      | Usually the same as `Authority`               |
| `Audiences`             | Yes      | Array of API Identifiers from Auth0 dashboard |
| `AllowMachineToMachine` | No       | Allow client credentials tokens               |
| `RequireHttpsMetadata`  | No       | Require HTTPS for metadata (default: true)    |
| `Diagnostics`           | No       | Dev-only diagnostics (no secrets)             |

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

// ========================================
// Middleware Pipeline (ORDER MATTERS!)
// ========================================
app.UseHttpsRedirection();
app.UseAuthentication();  // Must come before Authorization
app.UseAuthorization();

app.MapControllers();
app.Run();
```

---

## Step 5: Create a Protected Controller

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult GetPublic() => Ok(new { message = "This is public" });

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetMe() => Ok(new
    {
        userId = User.FindFirst("sub")?.Value,
        email = User.FindFirst("email")?.Value
    });
}
```

---

## Step 6: Get a Test Token from Auth0

### Option A: Using Auth0 Dashboard

1. Go to your API in Auth0 Dashboard
2. Click the **Test** tab
3. Copy the test token

### Option B: Using curl

```bash
curl --request POST \
  --url https://YOUR-TENANT.auth0.com/oauth/token \
  --header 'content-type: application/json' \
  --data '{
    "client_id": "YOUR-CLIENT-ID",
    "client_secret": "YOUR-CLIENT-SECRET",
    "audience": "https://your-api-identifier",
    "grant_type": "client_credentials"
  }'
```

### Option C: Using Auth0 Test Application

```bash
# Get token using Resource Owner Password flow (if enabled)
curl --request POST \
  --url https://YOUR-TENANT.auth0.com/oauth/token \
  --header 'content-type: application/json' \
  --data '{
    "client_id": "YOUR-TEST-APP-CLIENT-ID",
    "username": "test@example.com",
    "password": "testpassword",
    "audience": "https://your-api-identifier",
    "grant_type": "password",
    "scope": "openid profile email"
  }'
```

---

## Step 7: Test Your API

```bash
# Start your API
dotnet run

# Test public endpoint
curl http://localhost:5000/api/profile/public

# Test protected endpoint (should return 401)
curl http://localhost:5000/api/profile/private

# Test with valid token
curl http://localhost:5000/api/profile/private \
  -H "Authorization: Bearer YOUR-AUTH0-TOKEN"
```

---

## Working Example: Full API with Auth0

```csharp
using PrimusSaaS.Identity.Validator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure Auth0
builder.Services.AddPrimusIdentity(opts =>
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Public endpoint
app.MapGet("/", () => new { status = "healthy", auth = "Auth0" });

// Protected endpoint
app.MapGet("/me", [Authorize] (HttpContext ctx) =>
{
    var claims = ctx.User.Claims.Select(c => new { c.Type, c.Value });
    return new {
        authenticated = true,
        claims
    };
});

// Claims inspection endpoint
app.MapGet("/debug/claims", [Authorize] (HttpContext ctx) =>
{
    return ctx.User.Claims.Select(c => new {
        type = c.Type,
        value = c.Value
    });
});

app.Run();
```

### appsettings.json

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "Auth0",
        "Type": "Auth0",
        "Authority": "https://dev-example.auth0.com/",
        "Issuer": "https://dev-example.auth0.com/",
        "Audiences": ["https://my-api"]
      }
    ],
    "RequireHttpsMetadata": true,
    "Diagnostics": {
      "EnableDetailedErrors": true,
      "IncludeTokenHintsInChallenges": true,
      "IncludeDebugHeaders": true,
      "LogTokenRejectionReasons": true,
      "MaxRecentFailures": 50,
      "AutoDetectDevelopment": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## Troubleshooting

### Error: "Bearer error=invalid_token"

**Cause:** Token validation failed.

**Solutions:**

1. Verify Authority URL has trailing slash: `https://tenant.auth0.com/`
2. Verify Audience matches exactly what's in Auth0 dashboard
3. Check token hasn't expired
4. Ensure token was issued for correct audience

### Error: 401 with no message

**Cause:** Token not being sent or malformed.

**Solutions:**

```bash
# Verify token is being sent
curl -v http://localhost:5000/api/profile/private \
  -H "Authorization: Bearer YOUR-TOKEN"

# Check Authorization header format (must be "Bearer " with space)
```

### Error: "IDX10501: Signature validation failed"

**Cause:** Token signed with different key than expected.

**Solutions:**

1. Ensure Authority URL is correct
2. Check if using RS256 algorithm (Auth0 default)
3. Verify token wasn't modified

### Debug: Enable detailed errors (development only)

```json
{
  "PrimusIdentity": {
    "Diagnostics": {
      "EnableDetailedErrors": true
    },
    "Issuers": [...]
  }
}
```

---

## Auth0 Best Practices

1. **Use RS256 algorithm** (Auth0 default) - asymmetric signing
2. **Always validate audience** - prevents token reuse across APIs
3. **Use short token expiration** - 1 hour max for access tokens
4. **Store secrets securely** - use Azure Key Vault or similar
5. **Enable refresh tokens** for SPAs and mobile apps

---

## Next Steps

| Want to...                    | See Guide                                                     |
| ----------------------------- | ------------------------------------------------------------- |
| Add Azure AD as second issuer | [Multi-Issuer Setup ->](/docs/modules/identity-multi-issuer)  |
| Harden local/dev tokens       | [Local JWT Guide ->](/docs/modules/identity-local-jwt)        |
| Revisit basics quickly        | [Identity Quick Start ->](/docs/modules/identity-quick-start) |

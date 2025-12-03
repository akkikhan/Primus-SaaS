---
id: identity-azure-ad
title: Identity Validator - Azure AD Integration
sidebar_position: 3
description: Complete Azure AD (Entra ID) JWT validation setup with full examples.
---

# Azure AD / Entra ID Integration Guide

Complete guide to integrating Microsoft Entra ID (formerly Azure AD) authentication with your .NET API using Primus Identity Validator.

---

## Prerequisites

- Azure subscription with Entra ID tenant
- .NET 6.0+ project
- Permissions to register applications in Azure AD

---

## Step 1: Install Package

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

---

## Step 2: Register Your API in Azure Portal

### Create App Registration

1. Go to [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID**
2. Click **App registrations** → **New registration**
3. Fill in:
   - **Name**: `My API` (or your API name)
   - **Supported account types**: Choose based on your needs
   - **Redirect URI**: Leave blank for APIs
4. Click **Register**

### Note Your Values

After registration, note down:
- **Application (client) ID**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
- **Directory (tenant) ID**: `yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy`

### Expose an API

1. Go to **Expose an API**
2. Click **Add** next to Application ID URI
3. Accept default or customize: `api://your-client-id`
4. Click **Save**

### Add Scopes (Optional but Recommended)

1. Still in **Expose an API**, click **Add a scope**
2. Add scopes like:
   - `api://your-client-id/read` - Read access
   - `api://your-client-id/write` - Write access
   - `api://your-client-id/admin` - Admin access

---

## Step 3: Configure appsettings.json

```json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": true,
    "ValidateLifetime": true,
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "AzureAD",
        "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Issuer": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Audiences": [ "api://<CLIENT_ID>" ]
      }
    ],
    "Diagnostics": {
      "EnableInDevelopment": true,
      "IncludeTokenHints": true,
      "TrackFailures": true,
      "MaxTrackedFailures": 50
    }
  }
}
```

### Alternative: Multi-tenant (Azure AD common)

```json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": true,
    "Issuers": [
      {
        "Name": "AzureAD-Common",
        "Type": "AzureAD",
        "Authority": "https://login.microsoftonline.com/common/v2.0",
        "Issuer": "https://login.microsoftonline.com/common/v2.0",
        "Audiences": [ "api://<CLIENT_ID>" ]
      }
    ],
    "Diagnostics": {
      "EnableInDevelopment": true
    }
  }
}
```

### Configuration Reference

| Property | Required | Description |
|----------|----------|-------------|
| `Name` | Yes | Friendly name for logging |
| `Type` | Yes | `"AzureAD"` |
| `Authority` | Yes | `https://login.microsoftonline.com/<TENANT_ID>/v2.0` (or `common`/`organizations`) |
| `Issuer` | Yes | Same as authority for v2 endpoints |
| `Audiences` | Yes | Array of allowed audiences (e.g., `["api://<CLIENT_ID>"]`) |
| `RequireHttpsMetadata` | No | Defaults to true |
| `Diagnostics` | No | Dev-only diagnostics settings |

---

## Step 4: Complete Program.cs

```csharp
using PrimusSaaS.Identity.Validator;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// Register Primus Identity for Azure AD
// ========================================
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
public class UserController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult GetPublic() => Ok(new { message = "Public access" });

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetProfile() => Ok(new
    {
        objectId = User.FindFirst("oid")?.Value,
        upn = User.FindFirst("upn")?.Value,
        name = User.FindFirst("name")?.Value,
        email = User.FindFirst("preferred_username")?.Value,
        tenantId = User.FindFirst("tid")?.Value
    });
}
```

---

## Step 6: Get a Test Token

- Azure CLI (service-to-service):  
  `az account get-access-token --resource api://YOUR-CLIENT-ID --query accessToken -o tsv`

- Client credentials (curl):  
  `curl -X POST https://login.microsoftonline.com/YOUR-TENANT-ID/oauth2/v2.0/token -H "Content-Type: application/x-www-form-urlencoded" -d "client_id=YOUR-CLIENT-APP-ID" -d "client_secret=YOUR-CLIENT-SECRET" -d "scope=api://YOUR-API-CLIENT-ID/.default" -d "grant_type=client_credentials"`

---

## Step 7: Test Your API

```bash
# Start your API
dotnet run

# Test public endpoint
curl http://localhost:5000/api/user/public

# Test protected endpoint (should return 401)
curl http://localhost:5000/api/user/profile

# Test with valid Azure AD token
curl http://localhost:5000/api/user/profile \
  -H "Authorization: Bearer YOUR-AZURE-AD-TOKEN"
```

---

## Working Example: Full API with Azure AD

```csharp
using PrimusSaaS.Identity.Validator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure AD authentication
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

// Health check
app.MapGet("/", () => new { status = "healthy", auth = "Azure AD" });

// Get current user info`r`napp.MapGet("/me", [Authorize] (HttpContext ctx) => new {`r`n    objectId = ctx.User.FindFirst("oid")?.Value,`r`n    name = ctx.User.FindFirst("name")?.Value,`r`n    email = ctx.User.FindFirst("preferred_username")?.Value,`r`n    tenantId = ctx.User.FindFirst("tid")?.Value`r`n});`r`n`r`napp.Run();
```

### appsettings.json

```json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": true,
    "ValidateLifetime": true,
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "AzureAD",
        "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Issuer": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Audiences": [ "api://<CLIENT_ID>" ]
      }
    ],
    "Diagnostics": {
      "EnableInDevelopment": true
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

### Error: "AADSTS50011: Reply URL mismatch"

**Cause:** Redirect URI mismatch (for auth code flow).

**Solution:** Add correct redirect URI in app registration.

### Error: "AADSTS700016: Application not found"

**Cause:** Client ID doesn't exist in tenant.

**Solution:** Verify Client ID and Tenant ID are correct.

### Error: "AADSTS65001: User or admin hasn't consented"

**Cause:** Permissions not granted.

**Solution:** 
1. Go to API Permissions in Azure Portal
2. Click "Grant admin consent for [Tenant]"

### Error: "IDX10205: Issuer validation failed"

**Cause:** Token from different tenant.

**Solution:** 
- Check tenant ID matches
- For multi-tenant: use `common` or `organizations` endpoint

### Debug: Check token contents

```bash
# Decode JWT payload (base64)
echo "YOUR_JWT_PAYLOAD" | base64 -d | jq
```

Or use [jwt.ms](https://jwt.ms) to inspect tokens.

---

## Multi-Tenant Configuration

For apps accepting tokens from any Azure AD tenant:

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "AzureAD-MultiTenant",
        "Type": "AzureAD",
        "Authority": "https://login.microsoftonline.com/common/v2.0",
        "Issuer": "https://login.microsoftonline.com/common/v2.0",
        "Audiences": [ "api://YOUR-CLIENT-ID" ],
        "ValidateIssuer": false
      }
    ]
  }
}
```

⚠️ **Security Note**: When `ValidateIssuer` is false, validate tenant in your code:

```csharp
var tenantId = User.FindFirst("tid")?.Value;
var allowedTenants = new[] { "tenant-1-id", "tenant-2-id" };

if (!allowedTenants.Contains(tenantId))
{
    return Forbid();
}
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Add Auth0 as second issuer | [Multi-Issuer Setup ->](/docs/modules/identity-multi-issuer) |
| Harden local/dev tokens | [Local JWT Guide ->](/docs/modules/identity-local-jwt) |
| Revisit basics quickly | [Identity Quick Start ->](/docs/modules/identity-quick-start) |

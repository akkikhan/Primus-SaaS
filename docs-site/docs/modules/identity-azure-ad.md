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
    "Issuers": [
      {
        "Name": "AzureAD-Production",
        "Type": "AzureAd",
        "TenantId": "YOUR-TENANT-ID",
        "ClientId": "YOUR-CLIENT-ID",
        "ValidateAudience": true,
        "ValidateIssuer": true,
        "RequireHttpsMetadata": true
      }
    ],
    "DefaultScheme": "Bearer",
    "EnableDetailedErrors": false
  }
}
```

### Alternative: Using Authority URL

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "AzureAd",
        "Authority": "https://login.microsoftonline.com/YOUR-TENANT-ID/v2.0",
        "Audience": "api://YOUR-CLIENT-ID",
        "ValidateAudience": true,
        "ValidateIssuer": true
      }
    ]
  }
}
```

### Configuration Reference

| Property | Required | Description |
|----------|----------|-------------|
| `Name` | Yes | Friendly name for logging |
| `Type` | Yes | Must be `"AzureAd"` |
| `TenantId` | Yes* | Your Azure AD tenant ID |
| `ClientId` | Yes* | Application (client) ID |
| `Authority` | Alt | Full authority URL (alternative to TenantId) |
| `Audience` | Alt | API audience (defaults to `api://{ClientId}`) |
| `ValidateAudience` | No | Validate audience claim (default: true) |
| `ValidateIssuer` | No | Validate issuer claim (default: true) |

*Either `TenantId + ClientId` or `Authority + Audience` required.

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

// Alternative: Use convenience method
// builder.Services.AddPrimusIdentityForAzureAD(
//     tenantId: "YOUR-TENANT-ID",
//     clientId: "YOUR-CLIENT-ID");

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
    // Public endpoint
    [HttpGet("public")]
    public IActionResult GetPublic()
    {
        return Ok(new { message = "Public access" });
    }

    // Protected - requires valid Azure AD token
    [Authorize]
    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        // Azure AD standard claims
        var objectId = User.FindFirst("oid")?.Value;
        var upn = User.FindFirst("upn")?.Value;
        var name = User.FindFirst("name")?.Value;
        var email = User.FindFirst("preferred_username")?.Value;
        var tenantId = User.FindFirst("tid")?.Value;
        
        return Ok(new { 
            objectId,
            upn,
            name,
            email,
            tenantId
        });
    }

    // Check for specific Azure AD groups
    [Authorize]
    [HttpGet("admin")]
    public IActionResult GetAdmin()
    {
        var groups = User.FindAll("groups").Select(c => c.Value).ToList();
        
        // Check if user is in admin group
        var adminGroupId = "YOUR-ADMIN-GROUP-ID";
        if (!groups.Contains(adminGroupId))
        {
            return Forbid();
        }
        
        return Ok(new { message = "Admin access granted", groups });
    }
}
```

---

## Step 6: Get a Test Token

### Option A: Using Azure CLI

```bash
# Login to Azure
az login

# Get token for your API
az account get-access-token \
  --resource api://YOUR-CLIENT-ID \
  --query accessToken -o tsv
```

### Option B: Using Browser Interactive Flow

Create a test client app registration that can request tokens:

1. Register a new app in Azure AD (for testing)
2. Add **API permissions** for your API
3. Use the Authorization Code flow or Implicit flow

### Option C: Using Client Credentials (Service-to-Service)

```bash
curl -X POST \
  https://login.microsoftonline.com/YOUR-TENANT-ID/oauth2/v2.0/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=YOUR-CLIENT-APP-ID" \
  -d "client_secret=YOUR-CLIENT-SECRET" \
  -d "scope=api://YOUR-API-CLIENT-ID/.default" \
  -d "grant_type=client_credentials"
```

### Option D: Using MSAL.NET

```csharp
using Microsoft.Identity.Client;

var app = ConfidentialClientApplicationBuilder
    .Create("YOUR-CLIENT-APP-ID")
    .WithClientSecret("YOUR-CLIENT-SECRET")
    .WithAuthority(AzureCloudInstance.AzurePublic, "YOUR-TENANT-ID")
    .Build();

var result = await app.AcquireTokenForClient(
    new[] { "api://YOUR-API-CLIENT-ID/.default" })
    .ExecuteAsync();

Console.WriteLine(result.AccessToken);
```

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

// Get current user info
app.MapGet("/me", [Authorize] (HttpContext ctx) =>
{
    return new { 
        objectId = ctx.User.FindFirst("oid")?.Value,
        name = ctx.User.FindFirst("name")?.Value,
        email = ctx.User.FindFirst("preferred_username")?.Value,
        tenantId = ctx.User.FindFirst("tid")?.Value
    };
});

// List all claims (for debugging)
app.MapGet("/debug/claims", [Authorize] (HttpContext ctx) =>
{
    return ctx.User.Claims.Select(c => new { 
        type = c.Type, 
        value = c.Value 
    });
});

// Check group membership
app.MapGet("/check-group/{groupId}", [Authorize] (string groupId, HttpContext ctx) =>
{
    var groups = ctx.User.FindAll("groups").Select(c => c.Value).ToList();
    return new {
        requested = groupId,
        isMember = groups.Contains(groupId),
        allGroups = groups
    };
});

app.Run();
```

### appsettings.json

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "AzureAd",
        "TenantId": "YOUR-TENANT-ID",
        "ClientId": "YOUR-CLIENT-ID"
      }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## Azure AD Claims Reference

| Claim | Description | Example |
|-------|-------------|---------|
| `oid` | User's Object ID (unique per tenant) | `12345678-...` |
| `sub` | Subject (unique per app) | `abcdef-...` |
| `tid` | Tenant ID | `tenant-id-...` |
| `upn` | User Principal Name | `user@contoso.com` |
| `preferred_username` | Display username | `user@contoso.com` |
| `name` | Display name | `John Doe` |
| `email` | Email (if configured) | `john@example.com` |
| `groups` | Group IDs (if configured) | `["group-id-1", ...]` |
| `roles` | App roles | `["Admin", "User"]` |
| `scp` | Scopes (delegated) | `read write` |

---

## Enabling Group Claims

To include group claims in tokens:

1. Go to your API app registration
2. Click **Token configuration**
3. Click **Add groups claim**
4. Select **Security groups** (or All groups)
5. For Access tokens, choose **Group ID**

⚠️ **Note**: If user is in >150 groups, Azure AD sends a `hasgroups` claim instead. Use Microsoft Graph API for full group list.

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
        "Type": "AzureAd",
        "Authority": "https://login.microsoftonline.com/common/v2.0",
        "Audience": "api://YOUR-CLIENT-ID",
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
| Add Auth0 as second issuer | [Multi-Issuer Setup →](/docs/modules/identity-multi-issuer) |
| Use Azure AD groups for authorization | [Advanced Features →](/docs/modules/identity-advanced) |
| Integrate with Swagger UI | [Advanced Features →](/docs/modules/identity-advanced) |
| Full API reference | [Identity Validator Reference →](/docs/modules/identity-validator) |

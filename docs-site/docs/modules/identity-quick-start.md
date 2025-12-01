---
id: identity-quick-start
title: Identity Validator - Quick Start
sidebar_position: 1
description: Get JWT authentication working in 5 minutes with minimal code.
---

# Identity Validator - Quick Start

Get JWT authentication working in your .NET API in **under 5 minutes** with minimal code.

---

## 1. Install Package

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

---

## 2. Add Using Statement

```csharp
using PrimusSaaS.Identity.Validator;
```

---

## 3. Register in Program.cs

**Minimal setup (3 lines):**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Primus Identity
builder.Services.AddPrimusIdentity(opts => 
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapGet("/secure", () => "Authenticated!").RequireAuthorization();

app.Run();
```

---

## 4. Configure appsettings.json

Pick ONE issuer type below based on your identity provider:

### Option A: Auth0

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "Auth0",
        "Type": "Auth0",
        "Authority": "https://YOUR-TENANT.auth0.com/",
        "Audience": "https://your-api-identifier"
      }
    ]
  }
}
```

### Option B: Azure AD

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
  }
}
```

### Option C: Local JWT (Development)

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "Local",
        "Type": "Local",
        "Issuer": "https://localhost",
        "Audience": "my-api",
        "SigningKey": "your-256-bit-secret-key-min-32-chars!"
      }
    ]
  }
}
```

---

## 5. Test It

```bash
# Start your API
dotnet run

# Test unprotected endpoint
curl http://localhost:5000/

# Test protected endpoint (will return 401 without token)
curl http://localhost:5000/secure

# Test with token
curl http://localhost:5000/secure -H "Authorization: Bearer YOUR-JWT-TOKEN"
```

---

## That's It! 🎉

You now have JWT authentication working. Your API:
- ✅ Validates JWT tokens from your configured issuer
- ✅ Returns 401 for invalid/missing tokens
- ✅ Works with `[Authorize]` attribute on controllers

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Use Auth0 with full setup | [Auth0 Integration →](/docs/modules/identity-auth0) |
| Use Azure AD with full setup | [Azure AD Integration →](/docs/modules/identity-azure-ad) |
| Use Local JWT for development | [Local JWT Guide →](/docs/modules/identity-local-jwt) |
| Combine multiple issuers | [Multi-Issuer Setup →](/docs/modules/identity-multi-issuer) |
| Add advanced features | [Advanced Features →](/docs/modules/identity-advanced) |
| Troubleshoot issues | [Identity Validator Reference →](/docs/modules/identity-validator) |

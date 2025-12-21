---
id: identity-quick-start
title: Identity Validator - Quick Start
sidebar_position: 1
description: Get JWT authentication working in 5 minutes with minimal code.
---

# Identity Validator - Quick Start

> See also: [Identity Validator Overview](./identity-validator) for packages, OIDC/ JWT options, and Node.js examples.

Get JWT authentication working in your .NET API in **under 5 minutes** with the simplest local JWT setup.

:::info Complete Data Isolation
Primus Identity Validator runs **entirely within your application**. No tokens, user data, or credentials are ever transmitted to Primus servers. All JWT validation happens locally using your configured identity providers' public keys.
:::

import useBaseUrl from '@docusaurus/useBaseUrl';

<div className="download-grid">
  <a className="download-btn primary" href={useBaseUrl('/downloads/identity-minimal.zip')}>
    Minimal starter (.zip)
  </a>
  <a className="download-btn secondary" href={useBaseUrl('/downloads/identity-minimal-swagger.json')} download>
    Swagger (minimal)
  </a>
  <a className="download-btn primary" href={useBaseUrl('/downloads/identity-advanced.zip')}>
    Advanced starter (.zip)
  </a>
  <a className="download-btn secondary" href={useBaseUrl('/downloads/identity-advanced-swagger.json')} download>
    Swagger (advanced)
  </a>
</div>

---

## 1. Install Package

```bash
dotnet add package PrimusSaaS.Identity.Validator   # NuGet
```

---

## 2. Add Using Statement

```csharp
using Microsoft.AspNetCore.Authorization;
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Identity.Validator.Diagnostics;
```

---

## 3. Register in Program.cs

**Minimal setup (local JWT only):**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(opts =>
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts));
builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
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

// Diagnostics endpoint (dev only)
app.MapPrimusIdentityDiagnostics();

app.MapGet("/public", () => "public ok");
app.MapGet("/secure", [Authorize] () => "secure ok")
   .RequireAuthorization();

app.Run();
```

---

## 4. Configure appsettings.json (local dev)

Keep secrets in User Secrets/Key Vault-not in source control.

```json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": true,
    "ValidateLifetime": true,
    "ClockSkew": "00:05:00",
    "Issuers": [
      {
        "Name": "LocalDev",
        "Type": "Jwt",
        "Issuer": "https://localhost:5001",
        "Secret": "your-32-character-minimum-secret-key-here-1234",
        "Audiences": ["api://local-dev"]
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

---

## 5. Test It

```bash
# Start your API
dotnet run

# Test unprotected endpoint
curl http://localhost:xxxx/public

# Test protected endpoint (will return 401 without token)
curl http://localhost:xxxx/secure

# Test with token
curl http://localhost:xxxx/secure -H "Authorization: Bearer YOUR-JWT-TOKEN"
```

---

## That's It!

You now have JWT authentication working locally. Your API:

- Validates JWT tokens signed with your local dev secret
- Returns 401 for invalid/missing tokens
- Works with standard `[Authorize]`
- Ships with a dev-only diagnostics endpoint (`/primus/diagnostics`) that never returns secrets

---

## Next Steps

| Want to...                    | See Guide                                                   |
| ----------------------------- | ----------------------------------------------------------- |
| Use Auth0 with full setup     | [Auth0 Integration ->](/docs/modules/identity-auth0)         |
| Use Azure AD with full setup  | [Azure AD Integration ->](/docs/modules/identity-azure-ad)   |
| Use Local JWT for development | [Local JWT Guide ->](/docs/modules/identity-local-jwt)       |
| Combine multiple issuers      | [Multi-Issuer Setup ->](/docs/modules/identity-multi-issuer) |

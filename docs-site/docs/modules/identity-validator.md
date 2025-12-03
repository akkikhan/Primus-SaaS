---
id: identity-validator
title: Identity Validator
sidebar_position: 1
description: Minimal, local-first JWT/OIDC validation for .NET APIs with multi-issuer support.
---

# Identity Validator Module

Validate JWT/OIDC tokens locally in your .NET API. No tokens or user data are sent to Primus—everything runs in-process.

---

## Install

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

---

## Minimal setup (Local JWT)

**Program.cs**
```csharp
using Microsoft.AspNetCore.Authorization;
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Identity.Validator.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(opts =>
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts));
builder.Services.AddAuthorization();
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
app.MapPrimusIdentityDiagnostics(); // dev only

app.MapGet("/public", () => "public ok");
app.MapGet("/secure", [Authorize] () => "secure ok");

app.Run();
```

**appsettings.json**
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
        "Audiences": [ "api://local-dev" ]
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

---

## Downloads
- Minimal: [identity-minimal.zip](/downloads/identity-minimal.zip) (Swagger + Postman included)
- Advanced: [identity-advanced.zip](/downloads/identity-advanced.zip) (Swagger + Postman included)
- Static Swagger: [Minimal](/downloads/identity-minimal-swagger.json), [Advanced](/downloads/identity-advanced-swagger.json)

---

## Config reference (plain language)

**PrimusIdentity**
| Key | Type | Required | Description |
| --- | --- | --- | --- |
| `RequireHttpsMetadata` | bool | No (default true) | Require HTTPS for OIDC metadata fetches. |
| `ValidateLifetime` | bool | No (default true) | Validate token expiration. |
| `ClockSkew` | TimeSpan | No (default 5m) | Allowed clock drift when checking `exp/nbf`. |
| `JwksCacheTtl` | int (hours) | No | Cache duration for OIDC signing keys. |
| `Issuers` | array | Yes | List of identity providers the API accepts. |
| `Diagnostics` | object | No | Dev-only diagnostics; no secrets returned. |

**Issuers**
| Key | Type | Required | Description |
| --- | --- | --- | --- |
| `Name` | string | Yes | Friendly label (e.g., LocalDev, AzureAD, Auth0). |
| `Type` | enum | Yes | `Jwt`, `AzureAD`, `Auth0`, `Oidc`, `Google`, `Cognito`. |
| `Issuer` | string | Yes | Expected `iss` claim. For OIDC, usually matches Authority. |
| `Authority` | string | No (OIDC) | OIDC authority URL (used to fetch discovery/JWKS). |
| `JwksUrl` | string | No (OIDC) | Explicit JWKS URL override. |
| `Secret` | string | Yes for `Jwt` | HMAC secret (min 32 chars) for local/dev tokens. |
| `Audiences` | array | Yes | Allowed `aud` claim values. |

---

## Middleware order

```csharp
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
// app.MapPrimusIdentityDiagnostics(); // dev only
app.MapControllers();
```

---

## Troubleshooting (quick)

| Symptom | Likely cause | What to check |
|---------|--------------|---------------|
| 401 Unauthorized | Missing/invalid token | `Authorization: Bearer <token>` header. |
| Issuer not configured | `iss` mismatch | `Issuer` in config matches token `iss`. |
| Audience mismatch | `aud` mismatch | `Audiences` include the token’s `aud`. |
| Signature invalid | Wrong secret/JWKS | For `Jwt`, secret matches; for OIDC, authority/JWKS reachable. |
| Expired token | `exp` in past | Renew token; adjust `ClockSkew` only if clocks drift. |

In development, enable diagnostics and hit `/primus/diagnostics` to see non-secret config and recent failures.

---

## Version and compatibility (brief)
- NuGet package: `PrimusSaaS.Identity.Validator`
- Targets: .NET 6, 7, 8, 9
- Depends on ASP.NET Core JWT bearer (transitive)

See the version matrix for exact package versions. Further provider-specific setup lives in the Auth0, Azure AD, Local JWT, and Multi-Issuer pages.

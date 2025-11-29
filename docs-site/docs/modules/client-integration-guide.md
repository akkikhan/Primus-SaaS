---
id: client-integration-guide
title: Client Integration Guide (Identity + Logging)
description: End-to-end integration guide for Primus SaaS Identity Validator and Logging modules for Node.js and .NET.
---

Audience: first-time developers integrating Primus SaaS modules into their own APIs. Scope: latest verified package versions (Node.js and .NET), required dependencies, Azure AD setup, API surface, FAQs, and validation steps.

## 1) Overview
- **Identity Validator**: multi-issuer JWT/OIDC validation with RBAC and typed user context. Tokens are validated locally; no Primus service calls.
- **Logging**: structured logging with context enrichment, correlation IDs, timers, and console/file/Application Insights targets.
- **No hosted runtime**: all logic runs inside your app; Primus never stores user data or tokens.

## 2) Supported Stacks & Versions
| Module | Runtime | Package | Version | Notes |
|--------|---------|---------|---------|-------|
| Identity Validator | Node.js 16+ | `@primus-saas/identity-validator` | 1.3.2 | Express/NestJS middleware + direct validator |
| Identity Validator | .NET 6/7/8 | `PrimusSaaS.Identity.Validator` | 1.3.3 | ASP.NET Core authentication handler + helpers |
| Logging | Node.js 16+ | `@primus-saas/logging` | 1.2.2 | Structured logger + Express middleware |
| Logging | .NET 6/7/8 | `PrimusSaaS.Logging` | 1.2.2 | ILogger provider, middleware, file/App Insights targets |

## 3) Getting Started / Setup
### Prerequisites
- Azure subscription with permission to register applications.
- (Optional) Primus Portal application label for logging (`ApplicationId`). Identity Validator does **not** call Primus services.
- HTTPS-enabled environments for production.
- Ability to set environment variables or secrets (Key Vault, App Service settings, dotenv, User Secrets).

### Quick Install
```bash
# Identity Validator
npm install @primus-saas/identity-validator
dotnet add package PrimusSaaS.Identity.Validator

# Logging
npm install @primus-saas/logging
dotnet add package PrimusSaaS.Logging
```

### Environment Templates
**Node (.env)**
```bash
AZURE_TENANT_ID=<tenant-guid>
AZURE_API_AUDIENCE=api://<azure-client-id>
LOCAL_ISSUER=https://auth.local
LOCAL_JWT_SECRET=<32+char-secret>
LOCAL_AUDIENCE=api://local-app
PRIMUS_APP_ID=PSP-CLI-XXXXXX  # label for logging only
NODE_ENV=development
PORT=3000
```

**.NET (appsettings.Development.json or User Secrets)**
```json
{
  "Azure": {
    "TenantId": "<tenant-guid>",
    "ApiAudience": "api://<azure-client-id>"
  },
  "Local": {
    "Issuer": "https://auth.local",
    "Secret": "<32+char-secret>",
    "Audience": "api://local-app"
  },
  "PrimusLogging": {
    "ApplicationId": "PSP-CLI-XXXXXX"
  }
}
```
Use User Secrets for local development (`dotnet user-secrets set "Azure:TenantId" "<id>"`) and App Service settings/Key Vault in cloud.

## 4) Authentication & Authorization
- **Supported**: Azure AD (OIDC/JWKS), Local JWT (HS256), any OIDC-compliant issuer.
- **Credentials**:
  - Azure Portal → App registrations → your app → copy **Tenant ID** and **Application (client) ID**.
  - Certificates & secrets → **New client secret** (copy immediately).
  - Audience: App ID URI (e.g., `api://<client-id>`), must match token `aud`.
- **Roles**: ensure tokens include `roles` (or app roles) that match your authorization checks.
- **Secure storage**: env vars/Key Vault/User Secrets/App Service settings; never commit secrets.

Azure registration guide: https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app

## 5) Integration Steps (keep versions in table above for npm/NuGet parity)
### Node.js (Express/Nest)
```typescript
import express from 'express';
import { primusIdentityMiddleware, requireRoles } from '@primus-saas/identity-validator';

const app = express();

const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: `https://login.microsoftonline.com/${process.env.AZURE_TENANT_ID}/v2.0`,
      authority: `https://login.microsoftonline.com/${process.env.AZURE_TENANT_ID}/v2.0`,
      audiences: [process.env.AZURE_API_AUDIENCE]
    },
    {
      name: 'LocalAuth',
      type: 'jwt',
      issuer: process.env.LOCAL_ISSUER,
      secret: process.env.LOCAL_JWT_SECRET,
      audiences: [process.env.LOCAL_AUDIENCE]
    }
  ],
  clockSkew: 300,
  jwksCacheTtl: 24
});

app.get('/health', (_req, res) => res.json({ ok: true }));
app.get('/api/secure', primusAuth, (req, res) => res.json({ user: req.primusUser }));
app.get('/api/admin', primusAuth, requireRoles('Admin'), (_req, res) => res.json({ ok: true }));
app.listen(3000);
```

### .NET (ASP.NET Core)
```csharp
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Identity.Validator.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new()
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.Oidc,
            Issuer = $"https://login.microsoftonline.com/{builder.Configuration["Azure:TenantId"]}/v2.0",
            Authority = $"https://login.microsoftonline.com/{builder.Configuration["Azure:TenantId"]}/v2.0",
            Audiences = new() { builder.Configuration["Azure:ApiAudience"]! }
        },
        new IssuerConfig
        {
            Name = "LocalAuth",
            Type = IssuerType.Jwt,
            Issuer = builder.Configuration["Local:Issuer"]!,
            Secret = builder.Configuration["Local:Secret"]!,
            Audiences = new() { builder.Configuration["Local:Audience"]! }
        }
    };
    options.RequireHttpsMetadata = true;
    options.ClockSkew = TimeSpan.FromMinutes(5);
    // Dev: Allow HTTP on localhost while keeping HTTPS elsewhere
    options.AllowHttpOnLocalhost = true;
});

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/api/secure", [Authorize] (HttpContext ctx) =>
{
    var user = ctx.GetPrimusUser();
    return Results.Ok(new { user });
});
app.MapGet("/api/admin", [Authorize(Roles = "Admin")] () => Results.Ok(new { ok = true }));
app.Run();
```

### Add Logging
**Node**
```typescript
import express from 'express';
import { createLogger, LogLevel, primusLoggingMiddleware } from '@primus-saas/logging';

const app = express();
const logger = createLogger({
  applicationId: process.env.PRIMUS_APP_ID || 'APP-UNKNOWN',
  environment: process.env.NODE_ENV === 'production' ? 'production' : 'development',
  minLevel: LogLevel.INFO
});

app.use(primusLoggingMiddleware(logger));
app.get('/api/orders', (req, res) => {
  const timer = req.logger.startTimer();
  req.logger.info('Listing orders');
  timer.done('Orders fetched', { count: 0 });
  res.json({ items: [] });
});
app.listen(3000);
```

**.NET**
```csharp
using PrimusSaaS.Logging.Extensions;
using PrimusLogLevel = PrimusSaaS.Logging.Core.LogLevel;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = builder.Configuration["PrimusLogging:ApplicationId"] ?? "APP-UNKNOWN";
    options.Environment = builder.Environment.IsProduction() ? "production" : "development";
    options.MinLevel = PrimusLogLevel.Info;
    options.Targets = new()
    {
        new() { Type = "console", Pretty = builder.Environment.IsDevelopment() },
        new() { Type = "file", Path = "logs/app.log", Async = true }
    };
    options.Pii.MaskEmails = true;
    options.Pii.MaskCreditCards = true;
});

var app = builder.Build();
app.UsePrimusLogging();
app.MapGet("/health", () => Results.Ok(new { ok = true }));
app.Run();
```

### Validate
- Protected endpoint without token → `401 Unauthorized`.
- Protected endpoint with valid token from configured issuer → `200 OK` and user context.
- Logs:
  - Node: JSON entries include `requestId`, `method`, `path`, `duration`.
  - .NET: console/file/App Insights with HTTP context + correlation IDs.

## 6) API Surface (essentials)
- **Identity (Node)**: `primusIdentityMiddleware(config)`, `requireRoles(...roles)`, `PrimusIdentityValidator.validateToken(token)`, `req.primusUser`.
- **Identity (.NET)**: `AddPrimusIdentity(...)`, `[Authorize]` / `[Authorize(Roles="Role")]`, `HttpContext.GetPrimusUser()`, `TenantResolver`.
- **Logging (Node)**: `createLogger(options)`, `primusLoggingMiddleware(logger)`, `logger.startTimer()`, `logger.generateCorrelationId()`, level methods `debug|info|warn|error|critical`.
- **Logging (.NET)**: `AddPrimus`/`AddPrimusLogging`, `UsePrimusLogging`, `ILogger<T>` integration, file/console/App Insights targets, PII masking.

## 7) Use Cases & Examples
- Mixed Azure AD + Local JWT: configure multiple issuers; tokens routed by `iss`.
- Admin-only endpoints: Node `requireRoles('Admin')`; .NET `[Authorize(Roles="Admin")]`.
- Performance timing: Node `logger.startTimer()`; .NET timers from logging helpers.
- Correlation across services: generate correlationId and pass via `X-Correlation-ID`.
- Ready-to-run examples:
  - Node.js Express starter: `examples/nodejs-express` (.env.example).
  - ASP.NET Core starter: `examples/dotnet-api` (appsettings template in README).

## 8) Portal Application Details / PDF
- The Portal Application Details page mirrors this guide: install commands, env variables (with your `PrimusClientId`), config JSON/.env, and starter code per module/stack.
- “Copy all” and “Download PDF” in the portal pull from this same content; anchors point to this page (`/docs/modules/client-integration-guide` with `#5-integration-steps` for Identity and `#add-logging` for Logging).
- If you change package versions, update the portal docs base URL (`VITE_DOCS_BASE_URL`) and module metadata so the integration tab stays in sync.

## 9) Troubleshooting & FAQs
- No/invalid token → 401; ensure `Authorization: Bearer <token>`.
- Invalid signature/untrusted issuer → issuer/audience mismatch or secret mismatch.
- Token expired → re-issue token; adjust `clockSkew` for dev.
- Express missing → `npm install express` (peer dependency for middleware).
- PII masking → enable `options.Pii.*` in .NET; avoid logging secrets in Node.
- Where to store env vars → `.env` (local Node), User Secrets/appsettings (.NET), App Service settings/Key Vault in cloud.

## 10) Advanced / Customization
- Multi-issuer routing for any OIDC + JWT issuers.
- JWKS cache TTL (`jwksCacheTtl`, default 24h).
- Tenant resolution: map claims to tenant context via `TenantResolver` (.NET).
- Logging targets: console/file/App Insights (rotation and compression on .NET file target).
- Custom enrichers (Node/.NET) to attach tenant/user/request metadata.

## 11) Versioning
- Aligned with: Node Identity 1.3.2, .NET Identity 1.3.0, Node Logging 1.2.1, .NET Logging 1.2.1.
- SemVer: MAJOR breaking, MINOR features, PATCH fixes. Keep docs in sync with releases.

## 12) Validation Checklist
- [ ] Azure AD app registered; Tenant ID, Client ID, secret, audience noted.
- [ ] Environment variables set (no secrets in source control).
- [ ] Identity Validator installed and middleware registered.
- [ ] Protected endpoint returns 200 with valid token and 401 without.
- [ ] Role-based endpoint tested with role present/absent.
- [ ] Logging writes structured entries with requestId and duration.
- [ ] HTTPS enforced outside local development.
- [ ] Error logs monitored in staging before production.

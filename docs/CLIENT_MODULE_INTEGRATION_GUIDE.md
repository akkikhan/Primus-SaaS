# Primus SaaS Client Integration Guide (Identity + Logging)

Audience: first-time developers integrating Primus SaaS modules into their own APIs.  
Scope: latest verified package versions (.NET and Node.js), required dependencies, Azure AD credential setup, API surfaces, FAQs, and validation steps.

---

## 1) Overview
- **Identity Validator**: multi-issuer JWT/OIDC validation with RBAC and typed user context. Solves token validation across Azure AD + local issuers without routing traffic to Primus.
- **Logging**: structured logging with context enrichment, correlation IDs, timers, and console/file/App Insights targets. Solves consistent observability with PII masking.
- **No hosted runtime**: all logic runs inside your app; Primus never stores your user data or tokens.

## 2) Supported Stacks & Versions
| Module | Runtime | Package | Version | Notes |
|--------|---------|---------|---------|-------|
| Identity Validator | Node.js 16+ | `@primus-saas/identity-validator` | 1.3.1 | Express/NestJS middleware + direct validator |
| Identity Validator | .NET 6/7/8 | `PrimusSaaS.Identity.Validator` | 1.3.0 | ASP.NET Core authentication handler + helpers |
| Logging | Node.js 16+ | `@primus-saas/logging` | 1.1.1 | Structured logger + Express middleware |
| Logging | .NET 6/7/8 | `PrimusSaaS.Logging` | 1.2.1 | ILogger provider, middleware, file/App Insights targets |

---

## 3) Getting Started / Setup
### Prerequisites
- Azure subscription with permission to register applications.
- (Optional) Access to Primus Portal to pick an `ApplicationId` label for logging; Identity Validator does **not** call Primus services.
- HTTPS-enabled API environments (required for production).
- Ability to set environment variables or secret store (Key Vault, app service settings, dotenv).

### Quick Install
```bash
# Identity Validator
npm install @primus-saas/identity-validator
dotnet add package PrimusSaaS.Identity.Validator

# Logging
npm install @primus-saas/logging
dotnet add package PrimusSaaS.Logging
```

### Environment Templates (drop-in)
#### Node.js (.env)
```bash
AZURE_TENANT_ID=<tenant-guid>
AZURE_API_AUDIENCE=api://<azure-client-id>
LOCAL_ISSUER=https://auth.local
LOCAL_JWT_SECRET=<32+char-secret>
LOCAL_AUDIENCE=api://local-app
PRIMUS_APP_ID=PSP-CLI-XXXXXX
NODE_ENV=development
PORT=3000
```
`PRIMUS_APP_ID` is a logging label only; it is not an authentication secret.

#### .NET (appsettings.Development.json or User Secrets)
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
Use User Secrets for local (`dotnet user-secrets set "Azure:TenantId" "<id>"`) and App Service settings in Azure for production (`az webapp config appsettings set ...`).

---

## 4) Authentication & Authorization
- **Supported**: Azure AD (OIDC/JWKS), Local JWT (HS256), any OIDC-compliant issuer.
- **Credentials**:
  - Azure Portal → App registrations → your app → copy **Tenant ID** and **Application (client) ID**.
  - Create Client Secret: Certificates & secrets → New client secret (copy immediately).
  - Audience: use App ID URI (e.g., `api://<client-id>`); must match token `aud`.
- **Secure storage**: env vars, Key Vault/User Secrets/App Service settings. Never commit secrets.
- **Roles/Scopes**: Ensure tokens include `roles` (or app roles) matching your authorization checks.

Azure registration guide: https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app

---

## 5) Step-by-Step Integration
### Configure Identity Validator
#### Node.js (Express/Nest)
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

#### .NET (ASP.NET Core)
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
#### Node.js
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

#### .NET
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
- Protected endpoint with valid token from configured issuer → `200 OK` + user context.
- Logs:
  - Node: JSON entries with `requestId`, `method`, `path`, `duration`.
  - .NET: console/file/App Insights with HTTP context + correlation IDs.

---

## 6) Core Functionality / API Reference (essentials)
- **Identity Validator (Node)**  
  - `primusIdentityMiddleware(config: { issuers: IssuerConfig[]; clockSkew?; jwksCacheTtl? })` → Express middleware.  
  - `requireRoles(...roles: string[])` → Express middleware for RBAC.  
  - `PrimusIdentityValidator.validateToken(token: string)` → `{ isValid, claims, error }`.  
  - `req.primusUser` → user context (id, email, name, roles, additionalClaims).
- **Identity Validator (.NET)**  
  - `AddPrimusIdentity(Action<PrimusIdentityOptions>)` → registers authentication.  
  - `[Authorize]`, `[Authorize(Roles="Role")]` for RBAC.  
  - `HttpContext.GetPrimusUser()` → user context.  
  - `TenantResolver` delegate → map claims to tenant context.
- **Logging (Node)**  
  - `createLogger(options: LoggerOptions)`; level methods `debug|info|warn|error|critical`.  
  - `primusLoggingMiddleware(logger)` → attaches `req.logger` with enriched context + timers.  
  - `logger.startTimer()` and `logger.generateCorrelationId()`.
- **Logging (.NET)**  
  - `AddPrimus(options)` / `AddPrimusLogging(options)` → ILogger provider.  
  - `UsePrimusLogging()` → middleware adds HTTP context.  
  - Targets: console, file (path, async, rotation settings), Application Insights.  
  - PII masking: `options.Pii.*` booleans and custom keys.

---

## 7) Use Cases & Examples
- **Mixed Azure AD + Local JWT**: configure two issuers; tokens are routed by `iss`.
- **Admin-only endpoints**: Node `requireRoles('Admin')`; .NET `[Authorize(Roles="Admin")]`.
- **Performance measurement**: Node `logger.startTimer()`; .NET `Logger.StartTimer()` helper.
- **Correlation across services**: generate correlationId and pass via header `X-Correlation-ID`.
- Ready-to-run:
  - Node.js Express starter: `examples/nodejs-express` (.env.example included).
  - ASP.NET Core starter: `examples/dotnet-api` (appsettings template in README).

---

## 8) Troubleshooting & FAQs
- **No token / missing header** → returns 401; ensure `Authorization: Bearer <token>`.
- **Invalid signature / untrusted issuer** → check issuer/audience exactly matches token; ensure secrets match generator.
- **Token expired** → generate fresh token; adjust `clockSkew` for dev.
- **Express missing** → `npm install express` (peer dependency).
- **PII masking** → enable `options.Pii.*` (.NET); avoid logging secrets (Node).
- **Where to put env vars** → `.env` (local Node), User Secrets/appsettings (dotnet), App Service settings/Key Vault in cloud.

---

## 9) Advanced / Customization
- Multi-issuer routing (any OIDC + JWT issuers).
- JWKS cache TTL tuning (`jwksCacheTtl`, default 24h).
- Tenant resolution: map claims to tenant context via `TenantResolver` (.NET).
- Targets: add Application Insights (`Type = "applicationInsights"`) or rotate files with compression (see .NET README).
- Custom enrichers (Node/.NET) to attach tenant/user/request metadata.

---

## 10) Versioning
- Docs align with: Node Identity 1.3.1, .NET Identity 1.3.0, Node Logging 1.1.1, .NET Logging 1.2.1.
- Follow SemVer: MAJOR breaking, MINOR features, PATCH fixes.
- Keep docs versioned alongside releases (Docusaurus versioning recommended).

---

## 11) Validation Checklist
- [ ] Azure AD app registered; Tenant ID, Client ID, secret, and audience noted.
- [ ] Environment variables set (no secrets in source control).
- [ ] Identity Validator installed and middleware registered.
- [ ] Protected endpoint returns 200 with valid token and 401 without.
- [ ] Role-based endpoint tested with a role present/absent.
- [ ] Logging writes structured entries with requestId and duration.
- [ ] HTTPS enforced outside local development.
- [ ] Error logs monitored in staging before production cutover.

---

Need something else covered? Tell us your stack and we’ll tailor an example. Should we include Postman scripts or IaC snippets?***

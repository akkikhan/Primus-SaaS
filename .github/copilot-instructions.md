# Primus SaaS Platform - Copilot Instructions

## Architecture Overview

Primus SaaS is a **developer SDK platform** shipping reusable backend modules (Identity Validator, Logging, Notifications) as **NuGet and npm packages**. All logic runs client-side—no Primus-hosted runtime or PII storage.

### Key Components
| Component | Path | Purpose |
|-----------|------|---------|
| **SDK (.NET)** | `sdk/dotnet/` | `PrimusSaaS.Identity.Validator`, `Primus.Notifications` NuGet packages |
| **SDK (Node)** | `sdk/nodejs/`, `sdk/logging/nodejs/` | `@primus-saas/*` npm packages |
| **Portal** | `portal/backend/` (ASP.NET 8), `portal/frontend/` (React/Vite) | Internal admin control plane |
| **Live Demo** | `examples/LiveDemoApi/`, `examples/LiveDemoFrontend/` | Integration showcase app |
| **Docs Site** | `docs-site/` | Docusaurus public documentation |

## SDK Integration Patterns

### .NET Service Registration (Program.cs)
```csharp
// Identity: Multi-issuer JWT validation
builder.Services.AddPrimusIdentity(options => {
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

// Logging: Structured logging with PII masking
builder.Logging.AddPrimus(options => {
    builder.Configuration.GetSection("PrimusLogging").Bind(options);
});

// Notifications: Email/SMS with Liquid templates
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(opts => { /* config */ })
    .UseFileTemplates("NotificationTemplates")
    .UseLogger());
```

### Configuration Sections (appsettings.json)
- `PrimusIdentity` — Issuers array with `Type` (AzureAD/Auth0/LocalJwt), `Authority`, `Audience`
- `PrimusLogging` — Targets (Console/File/ApplicationInsights), `PiiOptions`, `CorrelationId`
- `Notifications:Smtp` / `Notifications:Twilio` — Provider credentials

## Build & Run Commands

```bash
# Full stack (Docker)
docker-compose up -d              # Portal at localhost:5173, API at localhost:5267

# Portal backend
cd portal/backend && dotnet run   # Runs on http://localhost:5267

# Portal frontend
cd portal/frontend && npm run dev # Runs on http://localhost:5173

# Live Demo
cd examples/LiveDemoApi && dotnet run        # API on http://localhost:5221
cd examples/LiveDemoFrontend && npm run dev  # Frontend on http://localhost:5173

# SDK Tests
dotnet test sdk/dotnet/PrimusSaaS.Identity.Validator.Tests/
cd sdk/nodejs/primus-identity-validator && npm test
```

## Project Conventions

### Package Versioning
- Packages follow SemVer: `1.3.6` (Identity), `1.2.3` (Logging), `1.4.2` (Notifications)
- Version bumps require updating `.csproj`/`package.json` AND `README.md` badges

### Test Structure
- .NET: xUnit tests in `*.Tests/` sibling projects
- Node: Jest tests colocated or in `__tests__/` directories
- CI runs on all PRs via `.github/workflows/ci.yml`

### Notification Templates
- Liquid templates in `NotificationTemplates/{Type}/{Channel}.liquid`
- Naming: `EmailSubject.liquid`, `EmailBody.liquid`, `SmsBody.liquid`

## Key Files Reference

| Pattern | Example File |
|---------|-------------|
| SDK Extension Entry | `sdk/dotnet/PrimusSaaS.Identity.Validator/PrimusIdentityExtensions.cs` |
| Multi-Issuer Config | `sdk/dotnet/PrimusSaaS.Identity.Validator/PrimusIdentityOptions.cs` |
| Notification Builder | `sdk/dotnet/Primus.Notifications/NotificationBuilder.cs` |
| Live Demo Integration | `examples/LiveDemoApi/Program.cs` (shows all 3 modules) |
| CI Pipeline | `.github/workflows/ci.yml` |

## Common Tasks

### Adding a New Issuer Type
1. Add enum in `IssuerType.cs` and options class in `sdk/dotnet/.../Models/`
2. Update `IssuerTypeExtensions.cs` for Authority/Audience resolution
3. Add middleware handling in `PrimusAuthenticationHandler.cs`
4. Add tests in `PrimusSaaS.Identity.Validator.Tests/`

### Publishing SDK Updates
1. Bump version in `.csproj` / `package.json`
2. Update CHANGELOG.md
3. Push to main — CI publishes to NuGet.org / npm via `publish-sdks.yml`

## Portal Admin Workflows

The **Portal** (`portal/backend` + `portal/frontend`) is the internal control plane—**not** a public runtime.

### Module Catalog Management

**Source of truth:** `portal/backend` database tables.

Portal admins can:
- **Register modules** — Name, category (Identity/Logging/Notifications), supported stacks (.NET/Node), status (`Preview|GA|Deprecated`)
- **Configure visibility flags** — Toggle module visibility in Live Demo and Docs Site
- **Maintain compatibility metadata** — Supported runtime versions, required config sections

### Documentation Generation Workflow

The Portal orchestrates docs generation runs:
- **Triggers:** SDK version publish (NuGet/npm) or manual "Regenerate Docs" button
- **Inputs:** SDK code annotations, module metadata, example snippets from `examples/`
- **Outputs:** Markdown files under `docs-site/docs/modules/`

**⚠️ Rule:** Generated docs under `docs-site/docs/generated/**` are **read-only**. Hand-written docs live under `docs-site/docs/manual/**`.

## Notification Templates & Liquid Conventions

### Folder Structure
```text
NotificationTemplates/
  PasswordReset/
    EmailSubject.liquid
    EmailBody.liquid
    SmsBody.liquid
  WelcomeEmail/
    EmailSubject.liquid
    EmailBody.liquid
  Partials/
    email_header.liquid
    email_footer.liquid
```

**Conventions:**
- One folder per notification type (e.g., `PasswordReset`, `WelcomeEmail`)
- One file per channel: `EmailSubject.liquid`, `EmailBody.liquid`, `SmsBody.liquid`
- Shared UI pieces in `Partials/`

### Standard Template Variables
| Variable | Description |
|----------|-------------|
| `{{ recipient.email }}` | Recipient email |
| `{{ recipient.name }}` | Recipient name (may be null) |
| `{{ app.name }}` | Application name |
| `{{ app.url }}` | Application base URL |
| `{{ data.code }}` | OTP / reset code |
| `{{ data.link }}` | Deep link / reset link |
| `{{ data.expiryMinutes }}` | Expiry time for copy |

### Template Best Practices
```liquid
{% if recipient.name %}
  Hi {{ recipient.name }},
{% else %}
  Hi there,
{% endif %}

{% include 'Partials/email_header' %}
...
{% include 'Partials/email_footer' %}
```

- Keep logic light — no business rules in templates
- Use `{{ app.url }}` instead of hardcoded URLs
- Test via Portal preview endpoint with JSON payload

## Debugging Multi-Issuer JWT Validation

Primus Identity supports **multiple issuers** (Azure AD, Auth0, LocalJwt) in one app. When validation fails:

### Quick Checklist
1. **Log token metadata** (NOT full token): `iss`, `aud`, `exp`
2. **Confirm issuer config** — Does `iss` match a configured `Authority`?
3. **Confirm audience** — Does `aud` match configured `Audience` for that issuer?
4. **Check clock skew** — NTP enabled? Allow 2–5 min skew max
5. **Environment mismatch** — Dev token against prod config is common failure

### Failure Categories
| Category | Meaning |
|----------|---------|
| `IssuerNotConfigured` | Token `iss` doesn't match any configured issuer |
| `AudienceMismatch` | Token `aud` doesn't match expected audience |
| `SignatureInvalid` | Token signature verification failed |
| `ExpiredOrNotYetValid` | Token `exp`/`nbf` outside allowed window |

### Debug Endpoint (Live Demo)
`/debug/jwt` — Protected endpoint that returns:
- Parsed claims (non-sensitive subset)
- Which issuer config was matched
- Validation result + failure reason

### Required Tests for New Issuer Types
Add to `PrimusSaaS.Identity.Validator.Tests/`:
1. **Happy path** — Valid token succeeds
2. **Wrong audience** — Same issuer, wrong `aud` → fails
3. **Wrong issuer** — Different `iss` → `IssuerNotConfigured`
4. **Expired token** — Expiration enforced

**⚠️ Never log full tokens or PII** — mask sensitive claims in non-prod logs.

## Configuration & Environment Variables

Primus SaaS uses **appsettings + environment variables**. Env vars always win.

### File Conventions
| File | Purpose |
|------|---------|
| `appsettings.json` | Base defaults |
| `appsettings.Development.json` | Local overrides |
| `.env.example` | Committed, **no secrets**, shows expected keys |
| `.env` / `.env.local` | Git-ignored, developer-specific |

### Example `.env.example`
```env
# Identity (Issuer 0 - Azure AD)
PRIMUS_IDENTITY__ISSUERS__0__TYPE=AzureAd
PRIMUS_IDENTITY__ISSUERS__0__AUTHORITY=https://login.microsoftonline.com/<tenant-id>/v2.0
PRIMUS_IDENTITY__ISSUERS__0__AUDIENCE=api://your-api-client-id

# Identity (Issuer 1 - Local JWT)
PRIMUS_IDENTITY__ISSUERS__1__TYPE=Local
PRIMUS_IDENTITY__ISSUERS__1__SIGNINGKEY=USE-USER-SECRETS-NOT-HERE

# Logging
PRIMUSLOGGING__APPLICATIONINSIGHTS__CONNECTIONSTRING=UseUserSecrets
PRIMUSLOGGING__MINIMUMLEVEL=Information

# Notifications
PRIMUS_NOTIFICATIONS__SMTP__HOST=smtp.example.com
PRIMUS_NOTIFICATIONS__SMTP__PORT=587
PRIMUS_NOTIFICATIONS__SMTP__USERNAME=do-not-commit-real-username
PRIMUS_NOTIFICATIONS__SMTP__USESSL=true

# Portal / Demo URLs
PORTAL__PUBLIC_URL=http://localhost:5173
LIVEDEMO__PUBLIC_URL=http://localhost:4200
```

### Precedence Rules
1. Environment variables (e.g., `PRIMUS_IDENTITY__...`)
2. `appsettings.{Environment}.json`
3. `appsettings.json`
4. Hardcoded defaults in options classes

### Git Hygiene
- `.env`, `.env.local`, and any file with real secrets **must be in `.gitignore`**
- **Locally:** Use `.env` OR `dotnet user-secrets` for secrets
- **In CI:** Map GitHub/DevOps secrets → env vars matching `.env.example` keys

**⚠️ Rule:** Never commit real secrets. Use `dotnet user-secrets` or CI secret injection.

## Golden Paths (Canonical Working Examples)

Every module includes a **Golden Path** — the fastest, cleanest way to make it work in a real application.

Each Golden Path must include:
1. NuGet / npm package install
2. Required imports
3. Minimal `Program.cs` (or Node equivalent)
4. Minimal `appsettings.json` or env vars
5. A working API endpoint or UI component that consumes the module
6. Example output (log entry, JWT claims, email delivered, etc.)

### Available Golden Paths
| Module | Golden Path | Output |
|--------|-------------|--------|
| **Identity Validator** | Protected `/whoami` endpoint | JWT claims returned |
| **Logging** | Custom field + structured log | Entry in Application Insights |
| **Notifications** | PasswordReset token flow | `EmailBody.liquid` rendered + sent |

Reference: `examples/LiveDemoApi/Program.cs` demonstrates all three modules integrated.

## Cross-Module Interaction Contracts

Modules must be **loosely coupled** — no direct dependencies. Allowed interactions only through:
- Shared event objects
- Shared JSON payloads
- Shared .NET options classes

### Allowed Interactions
| From | To | Via |
|------|-----|-----|
| Identity Validator | Logging | Structured log fields (`UserId`, `IssuerType`, `TenantId`) |
| Identity Validator | Notifications | `PasswordResetRequested` event |
| Logging | Portal | Structured log metrics for usage charts |

### Forbidden Interactions
- ❌ Identity Validator calls Notifications directly
- ❌ Notifications reads Identity DB tables
- ❌ Logging depends on module-specific types

**Boundary Rule:** If two modules must communicate, create an **event** or **contract**. No private coupling, no "quick hacks."

## Validation & Testing Matrix

Each module must ship with functional confidence across four layers:

| Layer | Required? | Tooling | Notes |
|-------|-----------|---------|-------|
| Unit Tests | ✔️ | xUnit/Jest | Validate pure logic |
| Integration Tests | ✔️ | WebApplicationFactory / Testcontainers | Validate configuration, DI, middleware |
| Golden Path Tests | ✔️ | Postman / Playwright | Validate canonical E2E example |
| Demo UI Tests | Optional | Cypress / Playwright | Validate Portal & LiveDemo interactions |

### What Must Be Tested for Every Module
- **Basic functionality works**
- **Misconfiguration yields meaningful errors**
- **No leaks of secrets, PII, or implementation details in logs**
- **Performance-sensitive pieces stay under expected latency**

## Non-Negotiable Anti-Patterns

The following patterns are **banned** across SDK, Portal, and modules:

- ❌ Hardcoding environment names
- ❌ Storing secrets anywhere in the repo
- ❌ Tight coupling between modules
- ❌ Silent failures (swallowing exceptions)
- ❌ Logging full JWTs, access tokens, session cookies, or PII
- ❌ Triggering user notifications inside controller actions (use service layer + events)

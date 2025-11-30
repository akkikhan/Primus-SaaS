# Primus SaaS Platform — Architecture Handbook

## 1. Purpose

This handbook defines how the Primus SaaS Platform is architected and how it must evolve as new modules are added.
The platform is built to scale horizontally: each capability is a separate module, packaged as SDKs (NuGet + npm),
integrated into customer applications via configuration and dependency injection.

Primus is **client-owned**: there is no Primus-hosted runtime and no PII or tenant business data stored by Primus.

---

## 2. System Overview

Core components:

| Component      | Location                                               | Responsibility                                      |
|---------------|--------------------------------------------------------|----------------------------------------------------|
| .NET SDK      | `sdk/dotnet/`                                          | Feature modules packaged as NuGet                  |
| Node SDK      | `sdk/nodejs/`                                          | Feature modules packaged as npm                    |
| Portal        | `portal/backend/`, `portal/frontend/`                  | Internal admin control plane                       |
| Live UI Demo  | `examples/LiveDemoApi/`, `examples/LiveDemoFrontend/`  | End-to-end, production-style demonstration         |
| Docs Site     | `docs-site/`                                           | Public documentation (Docusaurus)                  |
| Future Modules| new folders under `sdk/*`, `examples/*`, `portal/*`    | Additional horizontal capabilities                  |

The initial modules (Identity Validator, Logging, Notifications) are **examples of the pattern**, not special cases.
Every future module must follow the same architectural rules.

---

## 3. Module Design Contract

Every module in the platform (current and future) must satisfy the following layers:

| Layer          | Responsibility                                                    |
|----------------|-------------------------------------------------------------------|
| SDK            | Business logic, DI extensions, typed options                      |
| Configuration  | Strongly-typed options, env-first binding patterns                |
| Golden Path    | Minimal working example app/integration                           |
| Portal         | Metadata, visibility flags, compatibility information             |
| Docs           | Auto-generated reference + hand-written guides                    |
| Live UI Demo   | End-to-end UX surface that exercises the module                   |
| Tests          | Unit tests, integration tests, Golden Path tests                  |

If a proposed module cannot be expressed in this shape, it does not belong in the platform without redesign.

---

## 4. Integration Model

### 4.1 .NET Startup Pattern

All .NET integrations should follow the extension-method pattern from the SDKs and configuration binding:

```csharp
// Program.cs

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Identity: multi-issuer JWT validation
builder.Services.AddPrimusIdentity(o =>
    config.GetSection("PrimusIdentity").Bind(o));

// Logging: structured logging with PII masking
builder.Logging.AddPrimus(o =>
    config.GetSection("PrimusLogging").Bind(o));

// Notifications: email/SMS with Liquid templates
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(opts =>
    {
        // Bind from configuration or environment variables
    })
    .UseFileTemplates("NotificationTemplates")
    .UseLogger());
```

### 4.2 Configuration Sections

Required configuration sections (typically in `appsettings.json` overridden by env vars):

- `PrimusIdentity`  
  - An array of issuer definitions: `Type` (AzureAd/Auth0/LocalJwt/...), `Authority`, `Audience`, and any module-specific options.
- `PrimusLogging`  
  - Logging targets, PII options, correlation id behavior.
- `Notifications`  
  - Provider-specific settings (SMTP, Twilio, etc.) plus template location if applicable.

Configuration must always be **environment-driven** with environment variables overriding JSON.

---

## 5. Portal (Control Plane)

The Portal is the internal control plane of the platform. It is **not** a public runtime – it is where platform admins:

- Register and categorize modules.
- Control visibility in Docs Site and Live UI Demo.
- Maintain compatibility metadata (e.g., supported stacks, minimum versions).
- Trigger and monitor documentation generation.

### 5.1 Module Catalog

For each module, Portal stores:

- Name, description, and category.
- Supported tech stacks (e.g., `.NET`, `Node.js`).
- Status: `Preview`, `GA`, `Deprecated`, or `Internal`.
- Visibility flags:
  - Show in Live UI Demo?
  - Show in Docs Site?
- Compatibility metadata: minimum SDK versions, supported runtimes, required config sections.

### 5.2 Documentation Generation

Doc generation is orchestrated by the Portal and CI:

- Triggers:
  - SDK release (NuGet/npm publish) or
  - Manual "Regenerate Docs" in Portal.
- Inputs:
  - SDK code annotations (XML comments, TSdoc),
  - Module catalog metadata,
  - Example snippets from `examples/`.
- Outputs:
  - Markdown under `docs-site/docs/generated/**`.

Generated docs are **read-only**; hand-written guides live under `docs-site/docs/manual/**`.

---

## 6. Live UI Demo (First-Class Surface)

The Live UI Demo is a production-like showcase and acts as a truth-source for "does this module actually work end-to-end?"

Requirements for every module:

- Add an API demo route in `examples/LiveDemoApi` that exercises the module in a realistic scenario.
- Add a corresponding UI tile or page in `examples/LiveDemoFrontend` that calls the demo API and renders the result.
- Demonstrate:
  - Configuration in use,
  - A typical user flow,
  - Expected output,
  - Error states where relevant.

If a feature exists in Docs but not in Live UI Demo, the platform does **not** consider that feature fully shipped.

---

## 7. Notification Templates

Notifications use Liquid templates for channel-specific content.

### 7.1 Folder & Naming

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

- One folder per notification type (e.g., `PasswordReset`, `WelcomeEmail`).
- One file per channel: `EmailSubject.liquid`, `EmailBody.liquid`, `SmsBody.liquid`.
- Shared elements live under `Partials/`.

### 7.2 Standard Variables

Every template can rely on:

- **Recipient**
  - `{{ recipient.email }}`
  - `{{ recipient.name }}` (may be null)
- **Application**
  - `{{ app.name }}`
  - `{{ app.url }}`
- **Payload**
  - `{{ data.code }}` (OTP / reset code)
  - `{{ data.link }}` (deep link / reset link)
  - `{{ data.expiryMinutes }}` (for copy like “valid for 15 minutes”)

### 7.3 Template Rules

- Keep logic minimal: only formatting and light branching (`if`, `else`, `for`).
- No business rules inside templates; compute logic in code and pass a clean `data` object.
- Use partials for headers, footers, and shared blocks.
- Never hardcode environment URLs; always use `{{ app.url }}`.

---

## 8. Identity — Multi-Issuer JWT Validation

Identity Validator supports multiple issuers (e.g., Azure AD, Auth0, Local JWT) in a single app.

### 8.1 Validation Order

When validating a token:

1. Inspect `iss` and match it to a configured issuer (`Authority`).
2. Validate `aud` against the configured `Audience`.
3. Validate the signature using the appropriate key set.
4. Check `exp` and `nbf` (expiry / not-before) with reasonable clock skew.
5. Ensure the token belongs to the correct environment (dev tokens should not work in prod).

### 8.2 Debugging Guidelines

- Log only **metadata** in non-production environments:
  - Selected issuer type,
  - `iss`, `aud`, and token age,
  - Failure category such as `IssuerNotConfigured`, `AudienceMismatch`, `SignatureInvalid`, `ExpiredOrNotYetValid`.
- Never log full tokens or PII.

A debug endpoint such as `/debug/jwt` in Live Demo can help by showing:

- Parsed, non-sensitive claims,
- Which issuer configuration was used,
- Validation result and failure reason.

### 8.3 Tests for New Issuer Types

For each new issuer type, tests must cover:

- Valid token is accepted.
- Token with wrong audience is rejected.
- Token with wrong issuer maps to `IssuerNotConfigured` (or equivalent).
- Expired token is rejected.

---

## 9. Configuration & Environment Variables

Configuration is **environment-first**. The expected precedence:

1. Environment variables (e.g., `PRIMUS_IDENTITY__ISSUERS__0__TYPE`)
2. `appsettings.{Environment}.json`
3. `appsettings.json`
4. Hardcoded defaults (only for non-sensitive values).

### 9.1 File Conventions

- `appsettings.json` — base defaults.
- `appsettings.Development.json` — local overrides.
- `.env.example` — committed, no secrets, documents all required keys.
- `.env` / `.env.local` — developer-specific, git-ignored.

### 9.2 Example `.env.example`

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

# Portal / Live Demo URLs
PORTAL__PUBLIC_URL=http://localhost:5173
LIVEDEMO__PUBLIC_URL=http://localhost:4200
```

### 9.3 Git Hygiene

- `.env`, `.env.local`, and any file containing real secrets must be ignored via `.gitignore`.
- Local secrets should use `.env` and/or `dotnet user-secrets`.
- CI pipelines should inject secrets using the same environment variable names as defined in `.env.example`.

---

## 10. Golden Paths

Golden Paths are **canonical working examples** for each module. They are the fastest copy‑pasteable way to get a feature live.

Every module’s Golden Path must include:

1. Package installation (NuGet/npm).
2. Required imports and DI registration.
3. Minimal configuration (JSON + env vars).
4. A working endpoint or UI component.
5. Example output (claims, log entries, notifications, etc.).

Examples:

- Identity: `/whoami` endpoint that returns JWT claims when called with a valid token.
- Logging: request processed with structured log containing correlation id, user id, issuer type.
- Notifications: password-reset flow that renders and sends a notification using Liquid templates.

Golden Paths live in `examples/` and must be kept in sync with the latest SDK versions.

---

## 11. Cross-Module Interaction Contracts

Modules must remain loosely coupled to allow independent evolution.

Allowed interactions:

- Shared events (e.g., `PasswordResetRequested` event consumed by Notifications).
- Shared JSON contracts between services.
- Structured logging metadata (e.g., identity information added to logs).

Forbidden interactions:

- Direct references between modules that create tight coupling.
- Shared private database tables.
- Branching behavior based on “if other module is installed” checks.
- Using reflection to detect or invoke other modules.

If two modules need to communicate, define a clear event or contract instead of a hidden dependency.

---

## 12. Testing & Validation Matrix

Each module is considered production-ready only if it passes tests across multiple layers:

| Layer              | Required | Purpose                                       |
|--------------------|----------|-----------------------------------------------|
| Unit Tests         | ✔️       | Validate business logic                       |
| Integration Tests  | ✔️       | Validate DI configuration and infrastructure  |
| Golden Path Tests  | ✔️       | Validate real-world, end-to-end scenarios     |
| Live UI Demo Tests | Optional | Validate the user-facing demo experience      |

Key validation rules:

- Correct configuration results in successful behavior.
- Misconfiguration produces clear, actionable errors.
- No secrets, PII, or tokens are logged.
- Performance-sensitive modules stay within acceptable latency.

---

## 13. Non-Negotiable Anti-Patterns

These patterns are not allowed anywhere in the platform:

- Hardcoding secrets or environment names.
- Logging full JWT tokens, access tokens, refresh tokens, or session cookies.
- Logging PII in clear text (emails, names, IDs) beyond what is explicitly allowed.
- Tight coupling between modules.
- Swallowing exceptions or silently ignoring errors.
- Placing business logic inside Liquid templates.
- Implementing features only in Development that do not work in realistic environments.

---

## 14. Definition of Done (Module Level)

A module is considered **production-grade** only when:

- Its Golden Path works end-to-end.
- It has an integrated experience in the Live UI Demo.
- Configuration is fully environment-driven and documented via `.env.example`.
- Auto-generated docs build cleanly.
- Unit, integration, and Golden Path tests pass in CI.
- None of the anti-patterns above are present.

If any of these criteria fail, the response must be redesign, not patching.

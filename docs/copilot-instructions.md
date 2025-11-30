# Primus SaaS — Copilot Instructions

This file is for humans and AI assistants (e.g., GitHub Copilot, Cursor) working inside this repo.
It summarizes how the platform works and what rules must be followed when writing or modifying code.

---

## 1. What This Repo Contains

Primus SaaS is a **modular SaaS SDK platform**. It ships reusable backend modules as NuGet and npm packages.
Current modules include Identity Validator, Logging, and Notifications. Future modules must follow the same patterns.

Key components:

- `.NET SDK` → `sdk/dotnet/`
- `Node SDK` → `sdk/nodejs/`
- `Portal` → `portal/backend/`, `portal/frontend/`
- `Live UI Demo` → `examples/LiveDemoApi/`, `examples/LiveDemoFrontend/`
- `Docs Site` → `docs-site/`

There is **no Primus-hosted runtime** and **no PII storage** by Primus.

---

## 2. Integration Pattern (Copy This)

For .NET APIs, always register Primus modules using the extension methods and configuration binding:

```csharp
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddPrimusIdentity(o =>
    config.GetSection("PrimusIdentity").Bind(o));

builder.Logging.AddPrimus(o =>
    config.GetSection("PrimusLogging").Bind(o));

builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(opts =>
    {
        // Configure from appsettings / env vars
    })
    .UseFileTemplates("NotificationTemplates")
    .UseLogger());
```

Do **not** wire things manually if an extension method exists.

---

## 3. Configuration Rules

- Use strongly-typed options + configuration binding.
- Environment variables should override JSON.
- `.env.example` documents expected variables; `.env` and secrets are never committed.

Required sections:

- `PrimusIdentity` → issuers array (`Type`, `Authority`, `Audience`, etc.).
- `PrimusLogging` → targets, PII options, correlation behavior.
- `Notifications` → provider details (SMTP/Twilio/etc.).

When Copilot suggests config, it should match these shapes.

---

## 4. Golden Paths (Always Provide One)

Every module must have a **Golden Path**: a minimal, working example that a developer can copy to get the feature running.

For each module:

- Show package installation (NuGet/npm).
- Show DI registration in `Program.cs` or equivalent.
- Show minimal config (appsettings + env vars).
- Show at least one working endpoint or UI action.
- Show expected output (claims, logs, notifications, etc.).

Examples:

- Identity → `/whoami` returns validated claims.
- Logging → emits a structured log with correlation and identity metadata.
- Notifications → sends a password reset email using a Liquid template.

If you introduce a new module, **add its Golden Path**.

---

## 5. Live UI Demo Requirements

When changing or adding a module, also update the Live UI Demo:

- Add or update a demo route in `examples/LiveDemoApi` that uses the module.
- Add or update a UI tile/page in `examples/LiveDemoFrontend` to interact with that route.

If a feature is documented but has no Live UI Demo coverage, treat the feature as **not fully shipped**.

---

## 6. Notifications & Liquid Templates

Templates live under:

```text
NotificationTemplates/<Type>/<Channel>.liquid
```

Examples:

- `NotificationTemplates/PasswordReset/EmailSubject.liquid`
- `NotificationTemplates/PasswordReset/EmailBody.liquid`
- `NotificationTemplates/PasswordReset/SmsBody.liquid`

Standard variables:

- `{{ recipient.email }}`, `{{ recipient.name }}`
- `{{ app.name }}`, `{{ app.url }}`
- `{{ data.code }}`, `{{ data.link }}`, `{{ data.expiryMinutes }}`

Rules:

- Keep control flow simple: `if`, `else`, `for` only.
- No business logic: compute it in code and pass a clean `data` object.
- Use partials for shared content (headers/footers).
- Never hardcode environment URLs.

---

## 7. Multi-Issuer JWT Debugging (Checklist)

When debugging Identity validation failures, follow this order:

1. Check `iss` matches a configured issuer `Authority`.
2. Check `aud` matches the configured `Audience`.
3. Check signature is valid for that issuer.
4. Check `exp` and `nbf` for expiration / clock skew issues.
5. Confirm token belongs to the correct environment.

In non-production, log:

- Selected issuer type,
- `iss`, `aud`, token age (but not the full token),
- A clear failure category: `IssuerNotConfigured`, `AudienceMismatch`, `SignatureInvalid`, `ExpiredOrNotYetValid`.

Never log full tokens or PII.

---

## 8. Cross-Module Interaction Rules

- Modules must stay loosely coupled.
- Allowed:
  - Events (e.g., `PasswordResetRequested` → Notifications module).
  - Shared JSON request/response contracts.
  - Structured logging metadata.
- Forbidden:
  - Direct module-to-module references creating tight coupling.
  - Shared internal DB tables.
  - Reflection hacks to discover or invoke other modules.

If modules need to talk, define a contract or event – do not introduce hidden dependencies.

---

## 9. Testing Expectations

For each module, ensure:

- Unit tests exist for core logic.
- Integration tests cover DI + configuration.
- Golden Path scenario is tested.
- Live UI Demo behavior is at least manually validated; automated UI tests are a bonus.

Misconfigurations should produce clear, actionable errors, not silent failure.

---

## 10. Things You Must Not Do

- Do not log secrets, JWTs, access tokens, refresh tokens, or session cookies.
- Do not log PII in plain text without explicit justification.
- Do not hardcode environment names, URLs, or credentials.
- Do not add business logic to Liquid templates.
- Do not couple modules directly or reach into another module's private data store.
- Do not implement behavior that only works in Development.

---

## 11. Quick Self-Check Before Merging

Before shipping changes, ask:

- Does the module still follow the DI + config pattern?
- Is there a Golden Path, and does it still work?
- Does the Live UI Demo reflect the new/updated behavior?
- Are config keys consistent with `.env.example`?
- Did I avoid the anti-patterns listed above?

If any answer is “no”, fix that before merging.

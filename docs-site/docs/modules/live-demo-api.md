---
id: live-demo-api
title: Live Demo API Blueprint (.NET)
sidebar_position: 6
description: Generalized setup for a Primus SaaS Minimal API using the LiveDemo app as the reference template.
---

Use this as a starting point for any .NET Minimal API that needs Primus Identity, Logging, Notifications, Feature Flags, and Document Renderer. It’s distilled from `examples/LiveDemoApi/Program.cs` but written so you can drop it into new services.

## Packages to install

```bash
dotnet add package PrimusSaaS.Identity.Validator
dotnet add package PrimusSaaS.Logging
dotnet add package PrimusSaaS.Notifications
dotnet add package PrimusSaaS.FeatureFlags
dotnet add package Primus.Documents
```

## Baseline configuration (appsettings.*)

```json
{
  "PrimusLogging": {
    "MinimumLevel": "Information",
    "Targets": ["Console", "ApplicationInsights"],
    "ApplicationInsights": {
      "ConnectionString": "your-application-insights-connection-string"
    },
    "EnablePiiMasking": true
  },
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "AzureAd",
        "Issuer": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Audiences": [ "api://<CLIENT_ID>" ],
        "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Type": "Oidc"
      },
      {
        "Name": "LocalJwt",
        "Issuer": "https://auth.local",
        "Secret": "<LOCAL_DEV_SECRET_32+>",
        "Audiences": [ "api://primus-livedemo" ],
        "Type": "Jwt"
      }
    ]
  },
  "Notifications": {
    "Smtp": {
      "Host": "<SMTP_HOST>",
      "Port": 587,
      "Username": "<SMTP_USERNAME>",
      "Password": "<SMTP_PASSWORD>",
      "EnableSsl": true,
      "FromAddress": "no-reply@example.com",
      "FromName": "Primus Notifications"
    },
    "Twilio": {
      "AccountSid": "<TWILIO_SID>",
      "AuthToken": "<TWILIO_TOKEN>",
      "FromNumber": "<TWILIO_FROM>"
    }
  },
  "PrimusFeatureFlags": {
    "Enabled": true
  },
  "PrimusDocuments": {
    "Renderer": "Default"
  },
  "DemoLocalAuth": {
    "Email": "demo@primus.local",
    "Password": "PrimusDemo123!",
    "Name": "Local Demo User",
    "Subject": "local-demo-user"
  }
}
```

Store secrets in User Secrets/Key Vault/App Service settings; don’t commit them.

## Service registration (Program.cs)

```csharp
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Notifications;
using PrimusSaaS.Logging.Extensions;
using PrimusSaaS.FeatureFlags;
using Primus.Documents;

var builder = WebApplication.CreateBuilder(args);

// Logging + Application Insights
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(opts => builder.Configuration.GetSection("PrimusLogging").Bind(opts));
var ai = builder.Configuration["PrimusLogging:ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(ai) && ai != "your-application-insights-connection-string")
    builder.Services.AddApplicationInsightsTelemetry(o => o.ConnectionString = ai);

// Primus modules
builder.Services.AddPrimusIdentity(o => builder.Configuration.GetSection("PrimusIdentity").Bind(o));
builder.Services.AddPrimusNotifications(notifications =>
{
    var templatesPath = Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates");
    notifications.UseFileTemplates(templatesPath, validateOnStartup: true, watchForChanges: builder.Environment.IsDevelopment());
    notifications.UseLogger();
    notifications.UseInMemoryQueue(o =>
    {
        o.BoundedCapacity = 500;
        o.MaxParallelHandlers = 2;
        o.BaseRetryDelayMs = 250;
    });
    notifications.UseSmtp(builder.Configuration.GetSection("Notifications:Smtp"));
    notifications.UseTwilio(builder.Configuration, "Notifications:Twilio", validateOnStartup: false);
    notifications.ConfigureDispatch(o =>
    {
        o.ThrowOnFailure = true;
        o.FallbackToLogger = false;
        o.QueueOnFailure = false;
    });
});
builder.Services.AddPrimusFeatureFlags(o => builder.Configuration.GetSection("PrimusFeatureFlags").Bind(o));
builder.Services.AddPrimusDocumentRenderer(o => builder.Configuration.GetSection("PrimusDocuments").Bind(o));

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddPolicy("AllowFrontend", policy =>
    policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
          .AllowAnyHeader()
          .AllowAnyMethod()));
```

## Middleware order

```csharp
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UsePrimusLogging();   // request logging + correlation IDs
app.UseAuthentication();  // from AddPrimusIdentity
app.UseAuthorization();
```

Keep authentication/authorization between HTTPS redirection and your endpoints.

## Golden-path endpoints to keep or adapt

These come from the Live Demo; remove them for production or put them behind strong auth (and role checks) if you must keep them.

| Endpoint | Purpose |
| --- | --- |
| `GET /primus/diagnostics` | Issuer/config diagnostics (optional) |
| `GET /whoami` | Authenticated claims echo |
| `POST /log/test` | Logging demo with PII redaction |
| `POST /notifications/test` | Render `PasswordReset` template without sending |
| `GET /notifications/health` | Channel health snapshot |
| `POST /notifications/welcome` | Send welcome email (Email + Logger) |
| `POST /notifications/sms` | Send SMS via Twilio or logger |
| `POST /notifications/templates/preview` | Render template content safely |
| `GET/PUT /notifications/templates/{type}/{channel}` | Retrieve/update Liquid templates |
| `GET /notifications/templates` | List available templates |
| `GET /feature-flags/test` | Evaluate all flags for the current user |
| `GET /feature-flags/{flag}` | Evaluate a single flag (see Feature Flags module) |
| `POST /documents/render` | Render text/HTML/Markdown to PDF (direct bytes) |
| `POST /documents/render/link` | Render to PDF and return a one-time download token |
| `GET /documents/download/{token}` | Consume a token and download PDF |
| `POST /documents/self-test` | Renderer self-diagnostics (Basic/Extended) |
| `GET /telemetry/summary` | App Insights + process stats (dev-only) |
| `GET /logs/recent` | Tail the demo log file (dev-only; remove or lock down) |
| `POST /auth/auth0` | Auth0 client-credentials proxy (dev-only; remove) |
| `POST /auth/azure` | Azure CLI token proxy (dev-only; remove) |
| `POST /auth/local` | Local shared-secret JWT for demos |

## Production hardening checklist

- Remove `/auth/auth0`, `/auth/azure`, `/logs/recent`, and any demo secrets; if temporarily kept, require strict auth and audit.
- Disable `IdentityModelEventSource.ShowPII` outside local dev.
- Lock down CORS origins to real frontends; require HTTPS everywhere.
- Swap the in-memory notification queue for a durable provider in production.
- Store SMTP/Twilio secrets in Key Vault/User Secrets; never in `appsettings.json`.
- Protect or remove template management endpoints; require auth/roles on any that remain.

## Quick verification steps

- `GET /whoami` returns claims when authenticated; 401 when unauthenticated.
- `POST /notifications/test` renders templates without error.
- `GET /feature-flags/test` returns definitions with user context.
- `POST /documents/self-test` returns `success: true`.
- Application Insights receives request/dependency telemetry when configured.

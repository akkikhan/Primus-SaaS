---
id: identity-advanced
title: Identity Validator - Advanced Features
sidebar_position: 6
description: Swagger integration, diagnostics, and telemetry for Primus Identity Validator.
---

# Advanced Features

Add Swagger auth support, developer diagnostics, and telemetry for your Primus Identity setup.

---

## Swagger UI (Bearer)

```csharp
using Microsoft.OpenApi.Models;
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(opts =>
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts));
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter 'Bearer' + space + token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "ok");

app.Run();
```

---

## Diagnostics (dev only)

```csharp
builder.Services.AddPrimusIdentity(opts =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts);
    opts.Diagnostics = new DiagnosticsOptions
    {
        EnableInDevelopment = true,
        IncludeTokenHints = true,
        TrackFailures = true
    };
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Exposes /primus/diagnostics/* in development
if (app.Environment.IsDevelopment())
{
    app.MapPrimusIdentityDiagnostics();
}
```

Use the diagnostics to view sanitized config and recent auth failures during development. Disable in production.

---

## Telemetry (optional)

```csharp
builder.Services.AddPrimusIdentity(opts =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts);
    opts.EnableTelemetry = true; // emits traces/metrics
});
```

Add OpenTelemetry exporters (OTLP/Application Insights) if you want to collect the emitted traces/metrics.

---

## Next Steps

| Need | Go to |
|------|-------|
| Basic setup | [Quick Start](/docs/modules/identity-quick-start) |
| Auth0 | [Auth0 Integration](/docs/modules/identity-auth0) |
| Azure AD | [Azure AD Integration](/docs/modules/identity-azure-ad) |
| Local dev | [Local JWT](/docs/modules/identity-local-jwt) |
| Multi-issuer | [Multi-Issuer Setup](/docs/modules/identity-multi-issuer) |

---
id: dotnet-aspnet
title: .NET 8 (ASP.NET Core Minimal API)
---

Use the official `PrimusSaaS.Identity.Validator` package to validate Azure AD and LocalAuth tokens in the same pipeline.

## Install

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

## Program.cs

```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new()
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.Oidc,
            Issuer = "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0",
            Authority = "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0",
            Audiences = new() { "acc675f1-e32f-40b9-a0c6-716066cc6890" }
        },
        new IssuerConfig
        {
            Name = "LocalAuth",
            Type = IssuerType.Jwt,
            Issuer = "http://localhost:4000",
            Secret = builder.Configuration["Primus:LocalSecret"] ?? "local-dev-secret-123",
            Audiences = new() { "acc675f1-e32f-40b9-a0c6-716066cc6890" }
        }
    };

    options.RequireHttpsMetadata = false; // set true outside local dev
    options.ClockSkew = TimeSpan.FromSeconds(300);
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/public", () => Results.Ok(new { message = "No auth required" }));

app.MapGet("/api/protected", (HttpContext ctx) =>
{
    var user = ctx.GetPrimusUser();
    return Results.Ok(new
    {
        message = "Authenticated",
        user = new
        {
            user?.UserId,
            user?.Email,
            user?.Name,
            user?.Roles
        }
    });
}).RequireAuthorization();

app.MapGet("/api/admin", () => Results.Ok(new { message = "Admin only" }))
   .RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();
```

## Configuration from `appsettings.json`

```json
{
  "Primus": {
    "LocalSecret": "local-dev-secret-123"
  }
}
```

## Validation tips

- `RequireHttpsMetadata` should be `true` in cloud environments; only disable for local HTTP issuers.
- Audiences must exactly match the `aud` claim in your tokens; list multiple if needed.
- Inspect `HttpContext.GetPrimusUser()` in middleware/controllers to trace claim mapping during debugging.

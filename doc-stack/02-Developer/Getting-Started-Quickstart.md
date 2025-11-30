# 🚀 Getting Started — Quickstart Guide

**Time to Complete:** 15 Minutes  
**Prerequisites:** .NET 8 SDK, Node.js 20+, VS Code

---

## 1. 🏁 Introduction

Welcome to the **Primus SaaS Platform**. This guide will get you from "Zero" to "Hello World" with the core modules. You will set up a new .NET Web API and integrate Identity and Logging.

---

## 2. 📦 Installation

Create a new web API project:

```bash
dotnet new webapi -n MyPrimusApp
cd MyPrimusApp
```

Install the Primus SDKs (assuming local NuGet source or public feed):

```bash
dotnet add package PrimusSaaS.Identity.Validator
dotnet add package PrimusSaaS.Logging
```

---

## 3. 🔌 Integration

Open `Program.cs` and add the following lines. **Do not** manually configure JWT Bearer options; let Primus handle it.

```csharp
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Logging;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Identity
builder.Services.AddPrimusIdentity(o => 
    builder.Configuration.GetSection("PrimusIdentity").Bind(o));

// 2. Add Logging
builder.Logging.AddPrimus(o => 
    builder.Configuration.GetSection("PrimusLogging").Bind(o));

var app = builder.Build();

app.UseAuthentication(); // Required
app.UseAuthorization();

app.MapGet("/whoami", (System.Security.Claims.ClaimsPrincipal user) =>
{
    return Results.Ok(user.Claims.Select(c => new { c.Type, c.Value }));
})
.RequireAuthorization(); // Protect this endpoint

app.Run();
```

---

## 4. ⚙️ Configuration

Create a `.env` file in your project root (and add it to `.gitignore`!):

```env
# Identity: Trust a local test issuer (or Azure AD/Auth0)
PRIMUSIDENTITY__ISSUERS__0__TYPE=Local
PRIMUSIDENTITY__ISSUERS__0__AUTHORITY=https://localhost:5001
PRIMUSIDENTITY__ISSUERS__0__AUDIENCE=my-api
PRIMUSIDENTITY__ISSUERS__0__SIGNINGKEY=super-secret-key-for-dev-only-12345

# Logging
PRIMUSLOGGING__MINIMUMLEVEL=Information
PRIMUSLOGGING__ENABLECONSOLE=true
```

---

## 5. ▶️ Run & Verify

Run the app:
```bash
dotnet run
```

**Test it:**
1.  Generate a JWT signed with `super-secret-key-for-dev-only-12345`.
2.  Call `GET /whoami` with `Authorization: Bearer <token>`.
3.  **Success:** You see your claims JSON.
4.  **Logs:** Check your console. You should see structured logs with a `CorrelationId`.

🎉 **Congratulations!** You have successfully integrated the Primus Platform.

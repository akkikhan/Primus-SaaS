# Primus SaaS Identity Validator - .NET SDK

**Package version:** 1.5.0

Official .NET SDK for validating JWT/OIDC tokens from your configured identity providers (Azure AD, Auth0, Cognito, Google, or any JWT issuer). The package is library-only: no Primus-hosted login, no Primus-issued tokens, no outbound calls to Primus.

> Full client integration guide (Node + .NET + Logging): see `docs-site/docs/modules/client-integration-guide.md`.

---

## 📋 Requirements

### Supported Frameworks

| Framework | Status | JwtBearer Version | Notes |
|-----------|--------|-------------------|-------|
| .NET 9.0 | ✅ Supported | 9.0.11 | **Latest** - full feature parity |
| .NET 8.0 | ✅ Supported | 8.0.22 | Recommended LTS |
| .NET 7.0 | ✅ Supported | 7.0.20 | Full feature parity |
| .NET 6.0 | ✅ Supported | 6.0.36 | LTS - production ready |

> **Dependency Note:** Each target framework uses the matching `Microsoft.AspNetCore.Authentication.JwtBearer` version (e.g., .NET 9 projects pull JwtBearer 9.0.11). All frameworks share `System.IdentityModel.Tokens.Jwt` 8.14.0.

### SDK Requirements

> **Important:** The .NET SDK version must match or exceed your project's target framework.

| Your Project Targets | Required SDK | Download |
|---------------------|--------------|----------|
| .NET 9.0 | .NET SDK 9.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/9.0) |
| .NET 8.0 | .NET SDK 8.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| .NET 7.0 | .NET SDK 7.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/7.0) |
| .NET 6.0 | .NET SDK 6.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/6.0) |

**Check your SDK version:**
```bash
dotnet --version
```

**Common Issue:** If you see `NETSDK1045: The current .NET SDK does not support targeting .NET X.0`, install the matching SDK version above.

---

## Installation

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

Or via NuGet Package Manager:

```powershell
Install-Package PrimusSaaS.Identity.Validator
```

---

## 🚀 Auth0 Quick Start (5 Minutes)

New to Auth0? Follow these steps to secure your API in under 5 minutes.

### Step 1: Sign up for Auth0 (FREE)

1. Go to [https://auth0.com/signup](https://auth0.com/signup)
2. Create account (use Google/GitHub for fastest setup)
3. Choose a tenant name (e.g., `my-app` → `my-app.auth0.com`)

### Step 2: Create an API in Auth0

1. Go to **Dashboard → Applications → APIs → Create API**
2. **Name:** `My API` (or your app name)
3. **Identifier:** `https://my-api` (this becomes your Audience)
4. Click **Create**

### Step 3: Install the Package

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

### Step 4: Configure Your API (Program.cs)

```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add Auth0 authentication with one line
builder.Services.AddPrimusIdentity(options =>
{
    options.UseAuth0(
        domain: "my-app.auth0.com",      // From Step 1
        audience: "https://my-api");      // From Step 2

    // Allow client_credentials (M2M) tokens explicitly
    // options.Issuers[0].AllowMachineToMachine = true;
    // or: builder.Services.AddPrimusIdentityForAuth0("my-app.auth0.com", "https://my-api", allowMachineToMachine: true);
});

> Machine-to-machine tokens are **disabled by default**. If your Auth0 APIs issue `client_credentials` tokens, set `AllowMachineToMachine = true` (or use `AddPrimusIdentityForAuth0(..., allowMachineToMachine: true)`) to avoid 401s with "Machine-to-machine tokens are not allowed".

builder.Services.AddControllers();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapPrimusIdentityAuthDiagnostics(); // surfaces auth failure hints via X-Primus-Auth-Error header
app.Run();
```

### Step 5: Protect Your Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires valid Auth0 token
public class SecureController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { message = "Hello, authenticated user!" });
}
```

### Step 6: Get a Test Token

1. Go to **Dashboard → Applications → APIs → My API → Test tab**
2. Copy the test token
3. Test your API:

```bash
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" https://localhost:5001/api/secure
```

✅ **Done!** Your API is now secured with Auth0.

---

## 🆕 Modern Minimal API Integration (.NET 8/9)

This section shows how to wire the validator into modern minimal API and controller pipelines.

### Minimal API (Complete Example)

```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// 1. Services: Add Primus Identity with your preferred provider
builder.Services.AddPrimusIdentity(options =>
{
    // Option A: Auth0 (one-liner)
    options.UseAuth0("your-tenant.auth0.com", "https://your-api");

    // Option B: Azure AD
    // options.Issuers.Add(new IssuerConfig
    // {
    //     Name = "AzureAD",
    //     Type = IssuerType.AzureAD,
    //     Issuer = "https://login.microsoftonline.com/{tenant-id}/v2.0",
    //     Authority = "https://login.microsoftonline.com/{tenant-id}/v2.0",
    //     Audiences = { "api://your-api-id" }
    // });

    // Logging options (optional)
    options.Logging = new PrimusIdentityLoggingOptions
    {
        MinimumLevel = LogLevel.Information,
        RedactSensitiveData = true,
        LogValidationSteps = true
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// 2. Middleware: Order matters!
app.UseAuthentication();
app.UseAuthorization();

// 3. Diagnostics: Exposes auth failure hints (optional but recommended)
app.MapPrimusIdentityDiagnostics();

// 4. Endpoints: Use .RequireAuthorization() for minimal APIs
app.MapGet("/", () => "Hello, World!");

app.MapGet("/whoami", (HttpContext ctx) =>
{
    var user = ctx.GetPrimusUser();
    return Results.Ok(new
    {
        userId = user?.UserId,
        email = user?.Email,
        name = user?.Name,
        roles = user?.Roles,
        issuer = user?.Issuer
    });
}).RequireAuthorization();

app.MapGet("/admin", () => Results.Ok(new { message = "Admin access granted" }))
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();
```

### Controller-Based API (Complete Example)

```csharp
// Program.cs
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// Middleware pipeline
app.UseAuthentication();
app.UseAuthorization();

// Map controllers and diagnostics
app.MapControllers();
app.MapPrimusIdentityDiagnostics();

app.Run();
```

```csharp
// Controllers/SecureController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator;

[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult Public() => Ok(new { message = "Public endpoint" });

    [HttpGet("protected")]
    [Authorize]
    public IActionResult Protected()
    {
        var user = HttpContext.GetPrimusUser();
        return Ok(new
        {
            message = "Authenticated!",
            user = new { user?.UserId, user?.Email, user?.Roles }
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly() => Ok(new { message = "Admin access" });
}
```

### Configuration via appsettings.json

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "Auth0",
        "Type": "Oidc",
        "Issuer": "https://your-tenant.auth0.com/",
        "Authority": "https://your-tenant.auth0.com/",
        "Audiences": ["https://your-api"],
        "AllowMachineToMachine": true
      }
    ],
    "RequireHttpsMetadata": true,
    "ValidateLifetime": true,
    "ClockSkew": "00:05:00"
  }
}
```

### Middleware Order (Critical!)

```csharp
// CORRECT ORDER - Authentication must come before Authorization
app.UseAuthentication();    // 1. Validates JWT, sets HttpContext.User
app.UseAuthorization();     // 2. Checks policies, roles, claims

// WRONG - Authorization before Authentication will always fail
// app.UseAuthorization();
// app.UseAuthentication();
```

### Extension Methods Reference

| Method | Purpose |
|--------|---------|
| `services.AddPrimusIdentity(options)` | Register authentication services |
| `services.AddPrimusIdentityForAzureAD(tenantId, clientId)` | One-liner Azure AD setup |
| `services.AddPrimusIdentityForAuth0(domain, audience)` | One-liner Auth0 setup |
| `services.AddPrimusDevDiagnostics()` | Enable dev-mode diagnostics |
| `app.MapPrimusIdentityDiagnostics()` | Expose `/primus-identity/diagnostics` endpoint |
| `app.MapPrimusDevDiagnostics()` | Expose detailed dev diagnostics endpoints |
| `options.AddPrimusSwagger()` | Auto-configure Swagger security definitions |
| `HttpContext.GetPrimusUser()` | Get authenticated user info |
| `HttpContext.GetMatchedIssuer()` | Get which issuer validated the token |

---

## 🎯 Authorization

Primus Identity Validator handles **authentication** (validating JWT tokens). For **authorization** (roles, policies, claims), use ASP.NET Core's standard `[Authorize]` attribute.

### Usage Examples

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // Requires authentication only
    [HttpGet]
    [Authorize]
    public IActionResult GetUsers() => Ok();

    // Requires Admin OR Manager role
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult CreateUser() => Ok();

    // Requires custom policy
    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDeleteUsers")]
    public IActionResult DeleteUser(int id) => Ok();

    // Class-level with method override
    [HttpGet("public")]
    [AllowAnonymous]  // Override for public endpoint
    public IActionResult PublicEndpoint() => Ok();
}
```

---

## 🔧 One-Liner Provider Setup (v1.5.0+)

### Azure AD / Microsoft Entra ID

```csharp
// Handles both v1.0 (M2M) and v2.0 (interactive) issuers automatically
builder.Services.AddPrimusIdentityForAzureAD(
    tenantId: "your-tenant-id",
    clientId: "api://your-client-id",
    allowMachineToMachine: true);  // default: true
```

### Auth0

```csharp
builder.Services.AddPrimusIdentityForAuth0(
    domain: "your-tenant.auth0.com",
    audience: "https://your-api",
    allowMachineToMachine: true);  // default: false
```

---

## 📊 Swagger/OpenAPI Integration (v1.5.0+)

Auto-configure Swagger security definitions based on your Primus Identity issuers.

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // Auto-add security schemes for all configured issuers
    options.AddPrimusSwagger(primusOptions);
    
    // Or simple Bearer-only setup
    options.AddPrimusBearerSwagger();
    
    // Or Azure AD OAuth2 flow
    options.AddPrimusAzureAdSwagger(
        tenantId: "your-tenant-id",
        clientId: "your-client-id");
    
    // Or Auth0 OAuth2 flow
    options.AddPrimusAuth0Swagger(
        domain: "your-tenant.auth0.com",
        audience: "https://your-api");
});
```

---

## 🔍 Development Diagnostics (v1.5.0+)

Enhanced diagnostics for debugging authentication issues during development.

### Enable Dev Diagnostics

```csharp
builder.Services.AddPrimusDevDiagnostics(options =>
{
    options.EnableDetailedErrors = true;        // Detailed error messages
    options.IncludeTokenHintsInChallenges = true;  // Hints in WWW-Authenticate
    options.IncludeDebugHeaders = true;         // X-Primus-* debug headers
    options.LogTokenRejectionReasons = true;    // Log why tokens fail
    options.MaxRecentFailures = 100;            // Track last 100 failures
});

// Or auto-detect development environment
builder.Services.AddPrimusDevDiagnostics();  // Uses safe defaults in prod
```

### Map Diagnostics Endpoints

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Maps these endpoints under /_primus/diagnostics:
    // GET /_primus/diagnostics - Overview of auth configuration
    // GET /_primus/diagnostics/failures - Recent auth failures
    // GET /_primus/diagnostics/failures/stats - Failure statistics
    // POST /_primus/diagnostics/validate-token - Test token validation
    // DELETE /_primus/diagnostics/failures - Clear failure history
    app.MapPrimusDevDiagnostics();
}
```

### Diagnostics Options Presets

```csharp
// For development - all diagnostics enabled
options.Diagnostics = PrimusDiagnosticsOptions.ForDevelopment();

// For production - safe defaults, no sensitive info exposed
options.Diagnostics = PrimusDiagnosticsOptions.ForProduction();
```

### Example Diagnostics Response

```json
GET /_primus/diagnostics/failures
{
  "totalSinceStartup": 15,
  "count": 5,
  "failures": [
    {
      "timestamp": "2025-12-01T10:30:00Z",
      "reason": "IssuerNotConfigured",
      "reasonDescription": "Token issuer not found in configured issuers",
      "tokenIssuer": "https://wrong-issuer.auth0.com/",
      "configuredIssuers": ["Auth0", "AzureAD"]
    }
  ]
}
```

---

## Quick Start (General)

### 1. Configure in Program.cs or Startup.cs

```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Identity validation (multi-issuer)
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new()
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.AzureAD, // Alias for OIDC (Azure-friendly)
            Issuer = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
            Authority = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
            Audiences = new List<string> { "api://your-api-id" }
        },
        new IssuerConfig
        {
            Name = "LocalAuth",
            Type = IssuerType.Jwt,
            Issuer = "https://auth.yourcompany.com",
            Secret = "your-local-secret",
            Audiences = new List<string> { "api://your-api-id" }
        }
    };

    options.ValidateLifetime = true;
    options.RequireHttpsMetadata = true; // Set false for local dev only
    options.ClockSkew = TimeSpan.FromMinutes(5);

    // Optional: map claims to tenant context
    options.TenantResolver = claims => new TenantContext
    {
        TenantId = claims.Get("tid") ?? "default",
        Roles = claims.Get<List<string>>("roles") ?? new List<string>()
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

### Auth0 (simple helper)

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    // One-liner with sane defaults (issuer/audience/lifetime validation on)
    options.UseAuth0(
        domain: "your-tenant.auth0.com",
        audience: "https://your-api-identifier",
        auth0 =>
        {
            // Optional: map namespaced roles into [Authorize(Roles="...")]
            auth0.RoleClaimName = "https://your-api-identifier/roles";
            // Optional: require an organization claim/value
            auth0.ValidateOrganization = true;
            auth0.RequiredOrganization = "org_abc123";
        });

    // Add other providers alongside Auth0
    options.Issuers.Add(new IssuerConfig
    {
        Name = "AzureAD",
        Type = IssuerType.AzureAD,
        Issuer = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        Authority = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        Audiences = new List<string> { "api://your-api-id" }
    });
});
```

### Auth0 permissions/org policies

```csharp
builder.Services.AddAuthorization(options =>
{
    // Require ALL listed permissions
    options.RequireAuth0Permissions("CanManageClients", "read:clients", "write:clients");
    // Require ANY listed permission
    options.RequireAnyAuth0Permission("CanReadClients", "read:clients", "read:all");
    // Require an organization claim/value (set ValidateOrganization = true in Auth0 config)
    options.AddPrimusClaimPolicy("RequireOrg", "org_id");
});
```

### Auth0 permission attributes (controller-level)

```csharp
using PrimusSaaS.Identity.Validator;

[Auth0Permission("read:clients")]
public async Task<IActionResult> GetClients() { ... }

[Auth0Permissions("read:clients", "write:clients")]
public async Task<IActionResult> UpdateClient() { ... } // requires BOTH

[Auth0AnyPermission("read:clients", "read:all")]
public async Task<IActionResult> GetClient() { ... } // requires ANY

[RequireOrganization] // requires org claim (default org_id) to be present
public async Task<IActionResult> OrgScoped() { ... }

[RequireOrganization("org_abc123")] // requires specific org value
public async Task<IActionResult> SpecificOrgOnly() { ... }
```

### Google OIDC (ID tokens)

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.UseGoogle(audience: "<google-client-id>");
    // Add other providers as needed (AzureAD/Auth0/Local)
});

// AWS Cognito user pool
builder.Services.AddPrimusIdentity(options =>
{
    options.UseCognito(
        region: "us-east-1",
        userPoolId: "us-east-1_ABC123",
        audience: "<app-client-id>",
        cognito =>
        {
            cognito.RoleClaimName = "cognito:groups"; // optional, maps into ClaimTypes.Role
        });
});

// Machine-to-machine & email verification (Auth0 example)
builder.Services.AddPrimusIdentity(options =>
{
    options.UseAuth0("your-tenant.auth0.com", "https://your-api-identifier", auth0 =>
    {
        auth0.AllowMachineToMachine = true;
        auth0.AllowedGrantTypes.Add("client-credentials");
        auth0.AllowedMachineToMachineScopes.AddRange(new[] { "read:clients", "write:clients" });
        auth0.RequireEmailVerification = true; // for user tokens
    });
});
```

### M2M scope enforcement
- Set `AllowMachineToMachine = true` and `AllowedMachineToMachineScopes` to restrict scopes on client-credentials tokens.
- Tokens with scopes outside the allowed list will be rejected.

### Local/Test token generation

```csharp
// Generate a test JWT (HMAC) for local or integration tests
var token = TestTokenBuilder.Create()
    .WithIssuer("https://localhost")
    .WithAudience("api://your-api-id")
    .WithSecret("local-secret")  // match your IssuerConfig secret when validating
    .WithClaim("sub", "user-123")
    .WithClaim("email", "test@example.com")
    .Build();
```

### Using the built-in fake handler (for integration tests)

```csharp
// In your test host setup (WebApplicationFactory, minimal API, etc.)
services.AddFakePrimusAuth(); // from PrimusSaaS.Identity.Validator.Tests.IntegrationHarness
app.UseFakePrimusAuth();
```

This authenticates requests with a fixed user (`sub`, `email`, `name`) so you can test APIs without an external IdP. See `examples/dotnet-api/FakeAuthApi` for a runnable sample.

### Logging & diagnostics
- Configure logging verbosity and redaction via `options.Logging`:
  - `MinimumLevel` (default: Information)
  - `RedactSensitiveData` (default: true)
  - `LogValidationSteps` (default: true)
  - `LogClaimMapping` (default: false)
- The type is `PrimusIdentityLoggingOptions` (older docs mentioning `LoggingOptions` will not compile):
  ```csharp
  builder.Services.AddPrimusIdentity(options =>
  {
      options.Logging = new PrimusIdentityLoggingOptions
      {
          MinimumLevel = LogLevel.Information,
          RedactSensitiveData = true,
          LogValidationSteps = true,
          LogClaimMapping = false
      };
  });
  ```
- Expose diagnostics endpoint with `app.MapPrimusIdentityAuthDiagnostics();`
- Structured logging: when `LogValidationSteps` is true, issuer/audience/kid are logged; subjects are hashed when redaction is on.
- Refresh tokens: set `TokenRefresh.UseDurableStore = true` and register `IRefreshTokenStore` (e.g., `DistributedRefreshTokenStore` for Redis/SQL via `IDistributedCache`).

### Auth0 client_credentials (M2M) tokens
- Auth0 marks client credentials tokens with `gty: "client-credentials"` and a subject ending in `@clients`. These are treated as machine-to-machine tokens.
- To allow them, set `AllowMachineToMachine = true` in the Auth0 issuer config and optionally restrict `AllowedGrantTypes` to `client_credentials`.
  ```json
  {
    "Name": "Auth0",
    "Type": "Oidc",
    "Issuer": "https://your-tenant.us.auth0.com/",
    "Authority": "https://your-tenant.us.auth0.com/",
    "Audiences": [ "https://saas-api/" ],
    "AllowMachineToMachine": true,
    "AllowedGrantTypes": [ "client_credentials" ]
  }
  ```
- If `AllowMachineToMachine` is false, validation fails with "Machine-to-machine tokens are not allowed for this issuer."

### Multi-provider (Azure AD + Auth0 + Local)

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    // Azure AD
    options.Issuers.Add(new IssuerConfig
    {
        Name = "AzureAD",
        Type = IssuerType.AzureAD,
        Issuer = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        Authority = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        Audiences = new List<string> { "api://your-api-id" }
    });

    // Auth0 (one-line helper)
    options.UseAuth0("your-tenant.auth0.com", "https://your-api-identifier", auth0 =>
    {
        auth0.RoleClaimName = "https://your-api-identifier/roles"; // optional
        auth0.ValidateOrganization = true;
        auth0.RequiredOrganization = "org_abc123";
    });

    // Local JWT (shared secret)
    options.Issuers.Add(new IssuerConfig
    {
        Name = "LocalAuth",
        Type = IssuerType.Jwt,
        Issuer = "https://auth.yourcompany.com",
        Secret = "your-local-secret",
        Audiences = new List<string> { "api://your-api-id" }
    });
});
```

#### Azure AD issuer formats (v1 vs v2)
- Azure AD client_credentials (app-only) tokens default to **v1 issuers**: `https://sts.windows.net/{tenantId}/` (no `/v2.0`).
- User/interactive tokens typically use **v2 issuers**: `https://login.microsoftonline.com/{tenantId}/v2.0`.
- Configure `Issuer` to **match the token’s `iss`** claim, even if you still use the v2 authority for discovery/JWKS:
```csharp
options.Issuers.Add(new IssuerConfig
{
    Name = "AzureAD M2M",
    Type = IssuerType.AzureAD,
    Issuer = $"https://sts.windows.net/{tenantId}/",            // matches app-only tokens
    Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0", // discovery/JWKS
    Audiences = { "api://your-api-id" },
    AllowMachineToMachine = true
});
```
If you accept both interactive and client_credentials flows, add two issuer entries (v2 + v1) with different `Name` values but the same audience.

### Auth0 multi-tenant (resolve per request)

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.Auth0MultiTenant = new Auth0MultiTenantOptions
    {
        ResolveTenant = ctx =>
        {
            // Example: subdomain-based tenant routing
            var host = ctx.Request.Host.Host;
            return host.Split('.').FirstOrDefault();
        }
    };

    options.Auth0MultiTenant.Tenants["client-a"] = new Auth0Options
    {
        Domain = "client-a.auth0.com",
        Audiences = { "https://api-client-a" }
    };

    options.Auth0MultiTenant.Tenants["client-b"] = new Auth0Options
    {
        Domain = "client-b.auth0.com",
        Audiences = { "https://api-client-b" }
    };
});
```

### 2. Protect Your API Endpoints

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator;

[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    [HttpGet]
    [Authorize] // Requires valid token from a configured issuer
    public IActionResult GetSecureData()
    {
        // Get the authenticated Primus user
        var primusUser = HttpContext.GetPrimusUser();
        
        return Ok(new
        {
            message = "Secure data accessed successfully",
            user = new
            {
                userId = primusUser?.UserId,
                email = primusUser?.Email,
                name = primusUser?.Name,
                roles = primusUser?.Roles,
                issuer = primusUser?.Issuer,
                provider = primusUser?.ProviderName ?? primusUser?.ProviderType
            }
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")] // Requires Admin role from your IdP
    public IActionResult GetAdminData()
    {
        return Ok(new { message = "Admin-only data" });
    }
}
```

#### Get matched issuer/provider in controllers
The middleware stores the matched issuer for each request:
```csharp
using PrimusSaaS.Identity.Validator;

[HttpGet("whoami")]
[Authorize]
public IActionResult WhoAmI()
{
    var issuer = HttpContext.GetMatchedIssuer();
    return Ok(new
    {
        provider = issuer?.Provider ?? issuer?.Name ?? "unknown",
        issuer = issuer?.Issuer,
        audiences = issuer?.Audiences
    });
}
```
`PrimusUser` also surfaces `Issuer`, `ProviderName`, and `ProviderType` derived from these values.

### 3. Access User Information

```csharp
// In any controller or middleware
var primusUser = HttpContext.GetPrimusUser();

if (primusUser != null)
{
    Console.WriteLine($"User ID: {primusUser.UserId}");
    Console.WriteLine($"Email: {primusUser.Email}");
    Console.WriteLine($"Name: {primusUser.Name}");
    Console.WriteLine($"Roles: {string.Join(", ", primusUser.Roles)}");
    
    // Access additional claims
    foreach (var claim in primusUser.AdditionalClaims)
    {
        Console.WriteLine($"{claim.Key}: {claim.Value}");
    }
}
```

## Configuration Options

| Option | Required | Description | Default |
|--------|----------|-------------|---------|
| Issuers | Yes | List of issuer configs (Oidc/AzureAD or Jwt) | - |
| ValidateLifetime | No | Validate token expiration | true |
| RequireHttpsMetadata | No | Require HTTPS for metadata | true |
| AllowHttpOnLocalhost | No | Permit HTTP issuer/authority on localhost for development | true |
| ClockSkew | No | Allowed time difference | 5 minutes |
| JwksCacheTtl | No | JWKS cache TTL (OIDC) | 24 hours |
| TenantResolver | No | Map claims to TenantContext | null |

### IssuerConfig

| Field | Required | Description |
|-------|----------|-------------|
| Name | Yes | Friendly name (e.g., AzureAD, LocalAuth) |
| Type | Yes | Oidc or Jwt |
| Issuer | Yes | Expected iss value to route tokens |
| Authority | OIDC only | Authority URL for discovery/JWKS |
| JwksUrl | JWT optional | JWKS endpoint (if not using Secret) |
| Secret | JWT optional | Symmetric key for HMAC tokens |
| Audiences | Yes | Allowed audience values |
| ClaimMappings | No | Map provider claims to standard claim types |
| RoleClaimName | No | If set, mapped into ClaimTypes.Role |
| PermissionClaimName | No | Permission claim to normalize (defaults to `permissions`) |
| OrganizationClaimName | No | Organization claim to normalize (defaults to `org_id`) |
| ValidateOrganization | No | Require org claim presence (and optional value) |
| RequiredOrganization | No | Specific organization value to require when ValidateOrganization = true |

## Configuration from appsettings.json

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "Oidc",
        "Issuer": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Audiences": [ "api://your-api-id" ]
      },
      {
        "Name": "LocalAuth",
        "Type": "Jwt",
        "Issuer": "https://auth.yourcompany.com",
        "Secret": "your-local-secret",
        "Audiences": [ "api://your-api-id" ]
      }
    ],
    "RequireHttpsMetadata": false
  }
}
```

## Generating Tokens for Local JWT Issuer

> **Important:** The Secret, Issuer, and Audience values used when generating tokens MUST EXACTLY MATCH your validator configuration.

### Quick Example

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public string GenerateLocalJwtToken(string userId, string email, string name)
{
    // Critical: Load from same configuration source
    var secret = _config["PrimusIdentity:Issuers:1:Secret"];
    var issuer = _config["PrimusIdentity:Issuers:1:Issuer"];
    var audience = _config["PrimusIdentity:Issuers:1:Audiences:0"];
    
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(secret);
    
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim("sub", userId),
            new Claim("email", email),
            new Claim("name", name)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = issuer,
        Audience = audience,
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature
        )
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}
```

For complete token generation examples, see [TOKEN_GENERATION_GUIDE.md](./TOKEN_GENERATION_GUIDE.md)

## Troubleshooting

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| Invalid signature | Secret key mismatch | Ensure token generation and validation use the same secret |
| Untrusted issuer | Issuer format incorrect | Use full URL format (e.g., https://localhost:5265) not name |
| Invalid audience | Audience mismatch | Use API identifier format (e.g., api://your-app-id) |
| Token expired | Token past expiration | Generate new token or increase ClockSkew |

For detailed troubleshooting, see [ERROR_REFERENCE.md](./ERROR_REFERENCE.md)

## Migrating from JwtBearer/Auth0 SDK

- You can swap existing `AddJwtBearer` Auth0 config for `options.UseAuth0(domain, audience, ...)` without changing your controllers; permissions/roles map into standard claims.
- Auth0 namespaced roles: set `RoleClaimName` to your namespaced roles claim and `[Authorize(Roles = "...")]` will work.
- Permissions: use policies (`RequireAuth0Permissions/RequireAnyAuth0Permission`) or attributes (`Auth0Permission/Auth0Permissions/Auth0AnyPermission`).
- Minimal migration snippet:
  ```csharp
  // Before
  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(opt =>
      {
          opt.Authority = "https://your-tenant.auth0.com/";
          opt.Audience = "https://your-api-identifier";
      });

  // After
  builder.Services.AddPrimusIdentity(options =>
  {
      options.UseAuth0("your-tenant.auth0.com", "https://your-api-identifier", auth0 =>
      {
          auth0.RoleClaimName = "https://your-api-identifier/roles"; // if you had roles mapped
      });
  });
  builder.Services.AddAuthorization();
  ```
- Migration helper:
  ```csharp
  var auth0 = JwtBearerMigrationHelper.ToAuth0Options(new JwtBearerMigrationHelper.JwtBearerConfig
  {
      Authority = "https://your-tenant.auth0.com/",
      Audience = "https://your-api-identifier",
      RoleClaimName = "https://your-api-identifier/roles"
  });
  var options = new PrimusIdentityOptions();
  options.Issuers.Add(auth0.ToIssuerConfig());
  ```

## Production Deployment

> **Caution:** Never commit secrets to source control! Use Azure Key Vault or environment variables.

### Quick Checklist

- [ ] Secrets stored in Azure Key Vault
- [ ] RequireHttpsMetadata: true in production
- [ ] HTTPS redirection enabled
- [ ] CORS configured for production domains
- [ ] Logging and monitoring configured

For complete deployment guide, see [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md)

## Client Usage Example

To call your protected API from a client application:

```csharp
using System.Net.Http.Headers;


var httpClient = new HttpClient();
var jwtToken = "your-jwt-token-here"; // From your auth system or token generator


// Add token to Authorization header
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", jwtToken);

var response = await httpClient.GetAsync("https://your-api.com/api/secure");
var data = await response.Content.ReadAsStringAsync();
```

## Development Tips

### Disable HTTPS Requirement for Local Development

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.RequireHttpsMetadata = false; // Allow HTTP in development
    // ... other options
});
```

### Localhost HTTP shortcuts
- Loopback issuers/authorities such as `http://localhost:5000` are allowed by default for development (`AllowHttpOnLocalhost = true`).
- Set `AllowHttpOnLocalhost = false` to enforce HTTPS everywhere, even on localhost.

### Enable Detailed Logging

The SDK automatically logs authentication events to the console. For more detailed logging, enable ASP.NET Core logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication": "Debug"
    }
  }
}
```

## Requirements

- .NET 6.0, 7.0, 8.0, or 9.0
- ASP.NET Core 6.0, 7.0, 8.0, or 9.0

> **Dependency Matrix:** See the [Supported Frameworks](#supported-frameworks) table at the top for the exact `Microsoft.AspNetCore.Authentication.JwtBearer` version used per framework.

## ✅ Integration checklist (Auth0 + Azure AD)
- Auth0: Create an API (Machine-to-Machine Application) with `audience = https://your-api`. Enable **Client Credentials**. Docs: https://auth0.com/docs/get-started/auth0-overview/set-up-apis.
- Auth0 app settings to capture: `Domain` (issuer/authority), `Client ID/Secret`, `Audience`. M2M tokens use `gty: "client-credentials"` (hyphen) and `sub` ends with `@clients`.
- Azure AD: Register an app, expose API scopes or set `Application ID URI` (audience), and add a client app with `Client credentials`. Docs: https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app.
- Configuration (appsettings): set `Issuer`, `Authority`, and `Audiences` exactly; for Auth0 allow M2M with `AllowMachineToMachine = true` and include `AllowedGrantTypes: ["client_credentials", "client-credentials"]`.
- CORS: allow your frontend origin (e.g., `http://localhost:5173` / `https://localhost:5173`) on the API.

## Known Issues

### Namespace Conflict with PrimusSaaS.Logging

If you are using both `PrimusSaaS.Identity.Validator` and `PrimusSaaS.Logging`, you may encounter an ambiguous reference error for `UsePrimusLogging()`.

**Status:** Fixed in `PrimusSaaS.Logging >= 1.2.2` (duplicate extension removed). Simply import `using PrimusSaaS.Logging.Extensions;` and call:
```csharp
app.UsePrimusLogging();
```

**If you cannot upgrade Logging yet:** Use the fully qualified name or an alias.

```csharp
using PrimusLogging = PrimusSaaS.Logging.Extensions;

// ...

PrimusLogging.LoggingExtensions.UsePrimusLogging(app);
```

## Common pitfalls (save time)
- Logging options type is `PrimusIdentityLoggingOptions` (not `LoggingOptions`).
- Auth0 M2M: set `AllowMachineToMachine = true` and include both grant spellings: `client_credentials` and `client-credentials`.
- Azure AD v1 vs v2 issuers: client_credentials tokens often use `https://sts.windows.net/{tenantId}/` (v1). Ensure your `Issuer`/`Authority` matches the actual token issuer or add both.

## Documentation

- [TOKEN_GENERATION_GUIDE.md](./TOKEN_GENERATION_GUIDE.md) - Complete guide to generating JWT tokens
- [LOCAL_DEVELOPMENT_GUIDE.md](./LOCAL_DEVELOPMENT_GUIDE.md) - Setup guide for offline/local development
- [INTEGRATION_PATTERNS.md](./INTEGRATION_PATTERNS.md) - Controller, Service, and Middleware examples
- [TENANT_RESOLVER_GUIDE.md](./TENANT_RESOLVER_GUIDE.md) - Multi-tenant context resolution guide
- [PRIMUS_USER_REFERENCE.md](./PRIMUS_USER_REFERENCE.md) - PrimusUser object properties and mapping
- [ERROR_HANDLING_GUIDE.md](./ERROR_HANDLING_GUIDE.md) - Handling exceptions and customizing responses
- [ERROR_REFERENCE.md](./ERROR_REFERENCE.md) - Troubleshooting validation errors
- [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md) - Production deployment best practices
- [SECRET_MANAGEMENT.md](./SECRET_MANAGEMENT.md) - Securely managing secrets (Key Vault, User Secrets)
- [TESTING_GUIDE.md](./TESTING_GUIDE.md) - Testing guide with Postman & Integration Tests
- [CLAIMS_MAPPING.md](./CLAIMS_MAPPING.md) - Reference for required and optional claims
- [ANGULAR_INTEGRATION.md](./ANGULAR_INTEGRATION.md) - Integration guide for Angular applications

## Support

For issues, questions, or contributions, visit:
- GitHub: https://github.com/primus-saas/identity-validator
- Documentation: https://docs.primus-saas.com

## License

MIT License - see LICENSE file for details

---
id: identity-validator
title: Identity Validator
sidebar_position: 1
description: Enterprise-grade JWT/OIDC validation for .NET APIs with multi-issuer support, RBAC, and typed user context.
---

# Identity Validator Module

## 1. Module Overview

The **Primus Identity Validator** is an enterprise-grade JWT/OIDC validation library that runs entirely within your application. It provides multi-issuer authentication (Azure AD, Auth0, Google, Cognito, or local JWT), role-based access control (RBAC), permission-based authorization, and typed user context extraction.

**Key benefits:**
- **Multi-issuer support**: Validate tokens from Azure AD, Auth0, Google, Cognito, or local development JWT servers in the same application
- **Zero external calls**: All validation happens locally—Primus never stores or transmits your users' data
- **Built-in authorization**: `[PrimusAuthorize]` attributes for roles, permissions, and authenticated-only endpoints
- **Swagger integration**: One-liner methods to configure OAuth2/Bearer security in your OpenAPI docs
- **Developer diagnostics**: Dev-mode endpoints to debug token validation failures

---

## 2. Installation

### NuGet Package

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

**Current Version**: `1.5.0` (supports .NET 6, 7, 8, and 9)

See [Modules Version Matrix](/docs/modules/version-matrix) for the authoritative version list.

---

## 3. Required Using Statements

Add these using statements to your `Program.cs` or relevant files:

```csharp
// Core identity validation
using PrimusSaaS.Identity.Validator;

// For custom authorization attributes
using PrimusSaaS.Identity.Validator.Authorization;

// For Swagger/OpenAPI integration
using PrimusSaaS.Identity.Validator.Swagger;

// For dev-mode diagnostics (optional)
using PrimusSaaS.Identity.Validator.Diagnostics;
```

---

## 4. Program.cs Service Registration

### Option A: Full Configuration (Multi-Issuer)

Use this when you need Azure AD + Local JWT support or multiple identity providers:

```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Register Primus Identity with configuration binding
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

// Required: Add authorization services
builder.Services.AddAuthorization();

var app = builder.Build();

// Required: Add authentication and authorization middleware (see Section 6)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

### Option B: One-Liner for Azure AD Only

Use this simplified registration when Azure AD is your only identity provider:

```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// One-liner: Azure AD with tenant and client ID
builder.Services.AddPrimusIdentityForAzureAD(
    tenantId: builder.Configuration["AzureAd:TenantId"]!,
    clientId: builder.Configuration["AzureAd:ClientId"]!,
    allowMachineToMachine: true  // Enable client_credentials flow
);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

### Option C: Controller-Based API with Swagger

```csharp
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Identity.Validator.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Primus Identity
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

builder.Services.AddAuthorization();

// Add Swagger with Primus security schemes
builder.Services.AddSwaggerGen(c =>
{
    c.AddPrimusSwagger(builder.Services);  // Auto-configures OAuth2/Bearer from your issuers
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

---

## 5. Configuration (appsettings.json)

### Multi-Issuer Configuration (Azure AD + Local JWT)

```json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": true,
    "ValidateLifetime": true,
    "ClockSkew": "00:05:00",
    "JwksCacheTtl": 24,
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "AzureAD",
        "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
        "Issuer": "https://login.microsoftonline.com/{tenant-id}/v2.0",
        "Audiences": ["api://{client-id}"],
        "AllowMachineToMachine": true
      },
      {
        "Name": "LocalDev",
        "Type": "Jwt",
        "Issuer": "https://localhost:5001",
        "Secret": "your-32-character-minimum-secret-key-here",
        "Audiences": ["api://local-dev"]
      }
    ],
    "Diagnostics": {
      "EnableInDevelopment": true,
      "IncludeTokenHints": true,
      "TrackFailures": true,
      "MaxTrackedFailures": 50
    }
  }
}
```

### Azure AD Only Configuration

```json
{
  "AzureAd": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  }
}
```

### Configuration Options Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `RequireHttpsMetadata` | bool | `true` | Require HTTPS for OIDC metadata endpoints |
| `ValidateLifetime` | bool | `true` | Validate token expiration |
| `ClockSkew` | TimeSpan | `5 minutes` | Allowed clock drift for token validation |
| `JwksCacheTtl` | int | `24` | Hours to cache JWKS signing keys |
| `Issuers` | array | `[]` | List of configured identity providers |

### Issuer Configuration Reference

| Option | Type | Description |
|--------|------|-------------|
| `Name` | string | Friendly name (e.g., "AzureAD", "LocalAuth") |
| `Type` | enum | `AzureAD`, `Oidc`, `Auth0`, `Google`, `Cognito`, `Jwt` |
| `Authority` | string | OIDC authority URL (for OIDC-based issuers) |
| `Issuer` | string | Expected `iss` claim value |
| `JwksUrl` | string | (Optional) Direct JWKS endpoint URL |
| `Secret` | string | (For Jwt type) Shared secret key (min 32 chars) |
| `Audiences` | array | Valid `aud` claim values |
| `AllowMachineToMachine` | bool | Enable client_credentials flow |
| `RoleClaimName` | string | Custom claim name for roles |
| `PermissionClaimName` | string | Custom claim name for permissions |

---

## 6. Middleware Pipeline Order

**Critical**: The middleware order matters. Use this exact sequence:

```csharp
var app = builder.Build();

// 1. Exception handling (first)
app.UseExceptionHandler("/error");

// 2. HTTPS redirection
app.UseHttpsRedirection();

// 3. Static files (if any)
app.UseStaticFiles();

// 4. Routing
app.UseRouting();

// 5. CORS (before auth)
app.UseCors();

// 6. Authentication (validates tokens)
app.UseAuthentication();

// 7. Authorization (enforces policies)
app.UseAuthorization();

// 8. Dev diagnostics (optional, development only)
if (app.Environment.IsDevelopment())
{
    app.MapPrimusDevDiagnostics();  // Adds /_primus/diagnostics/* endpoints
}

// 9. Endpoints
app.MapControllers();

app.Run();
```

---

## 7. Required Dependencies

The package automatically includes these dependencies:

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 6.0.36–9.0.11* | JWT Bearer authentication |
| `Microsoft.IdentityModel.Tokens` | (transitive) | Token validation |
| `System.IdentityModel.Tokens.Jwt` | (transitive) | JWT parsing |
| `Swashbuckle.AspNetCore.SwaggerGen` | 6.6.2 | Swagger integration |
| `Microsoft.SourceLink.GitHub` | 8.0.0 | Source debugging |

*Version varies by target framework (.NET 6/7/8/9)

### Additional Framework Dependencies

No additional packages are required. All dependencies are bundled with the NuGet package.

---

## 8. External Guides & Resources

### Azure AD Setup
- [Register an application with Microsoft Identity Platform](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app)
- [Configure app roles in Azure AD](https://learn.microsoft.com/azure/active-directory/develop/howto-add-app-roles-in-azure-ad-apps)
- [Azure AD v2.0 tokens reference](https://learn.microsoft.com/azure/active-directory/develop/access-tokens)

### Auth0 Setup
- [Auth0 .NET Web API Quickstart](https://auth0.com/docs/quickstart/backend/aspnet-core-webapi)
- [Auth0 RBAC configuration](https://auth0.com/docs/manage-users/access-control/rbac)

### General JWT/OIDC
- [JWT.io Debugger](https://jwt.io/) — Decode and inspect tokens
- [OpenID Connect Specification](https://openid.net/specs/openid-connect-core-1_0.html)

---

## 9. End-to-End Working Example

### Complete Minimal API Example

Create a new project and follow these steps:

**Step 1: Create project and install package**
```bash
dotnet new webapi -n MySecureApi
cd MySecureApi
dotnet add package PrimusSaaS.Identity.Validator
```

**Step 2: Replace `Program.cs`**
```csharp
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Identity.Validator.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Register Primus Identity
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Public endpoint - no auth required
app.MapGet("/", () => "Hello, World!");

// Protected endpoint - requires valid token
app.MapGet("/whoami", [Authorize] (ClaimsPrincipal user) =>
{
    return new
    {
        UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        Email = user.FindFirst(ClaimTypes.Email)?.Value,
        Name = user.FindFirst(ClaimTypes.Name)?.Value,
        Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
        Issuer = user.FindFirst("iss")?.Value
    };
});

// Admin-only endpoint - requires "Admin" role
app.MapGet("/admin", [PrimusAuthorizeRoles("Admin")] () => 
    new { Message = "Welcome, Admin!" });

// Permission-based endpoint
app.MapGet("/reports", [PrimusAuthorizePermissions("reports:read")] () =>
    new { Message = "Here are your reports" });

app.Run();
```

**Step 3: Add `appsettings.json` configuration**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "PrimusIdentity": {
    "RequireHttpsMetadata": false,
    "ValidateLifetime": true,
    "ClockSkew": "00:05:00",
    "Issuers": [
      {
        "Name": "LocalDev",
        "Type": "Jwt",
        "Issuer": "https://localhost:5001",
        "Secret": "this-is-a-32-character-secret-key!",
        "Audiences": ["api://my-secure-api"]
      }
    ]
  }
}
```

**Step 4: Run and test**
```bash
dotnet run
```

**Step 5: Test with curl**
```bash
# Public endpoint (no token needed)
curl https://localhost:5001/

# Generate a test JWT at jwt.io with:
# - Header: {"alg": "HS256", "typ": "JWT"}
# - Payload: {"iss": "https://localhost:5001", "aud": "api://my-secure-api", "sub": "user-123", "exp": 9999999999}
# - Secret: this-is-a-32-character-secret-key!

# Protected endpoint
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" https://localhost:5001/whoami
```

### Complete Controller-Based Example

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator.Authorization;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // GET /api/users/me - Requires any authenticated user
    [HttpGet("me")]
    [PrimusAuthenticated]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        
        return Ok(new { userId, email, roles });
    }

    // GET /api/users - Admin only
    [HttpGet]
    [PrimusAuthorizeRoles("Admin")]
    public IActionResult GetAllUsers()
    {
        return Ok(new[] { 
            new { Id = "1", Name = "Alice" },
            new { Id = "2", Name = "Bob" }
        });
    }

    // DELETE /api/users/{id} - Requires 'users:delete' permission
    [HttpDelete("{id}")]
    [PrimusAuthorizePermissions("users:delete")]
    public IActionResult DeleteUser(string id)
    {
        return Ok(new { Message = $"User {id} deleted" });
    }

    // POST /api/users/bulk - Requires Admin OR Manager role
    [HttpPost("bulk")]
    [PrimusAuthorizeRoles("Admin", "Manager")]
    public IActionResult BulkOperation()
    {
        return Ok(new { Message = "Bulk operation completed" });
    }
}
```

---

## 10. Troubleshooting

### Common Issues and Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| `401 Unauthorized` with no details | Token missing or malformed | Check `Authorization: Bearer <token>` header format |
| `IssuerNotConfigured` | Token `iss` claim doesn't match any configured issuer | Verify `Issuer` value in appsettings matches token's `iss` exactly |
| `AudienceMismatch` | Token `aud` doesn't match configured audiences | Add the token's `aud` value to `Audiences` array |
| `SignatureInvalid` | Wrong signing key or algorithm | For Jwt type: verify `Secret` matches; For OIDC: check JWKS URL |
| `ExpiredOrNotYetValid` | Token expired or `nbf` in future | Check server clock sync; increase `ClockSkew` if needed |
| JWKS fetch fails | Network or firewall issue | Verify `Authority` URL is accessible from server |

### Debug Token Validation in Development

Enable dev diagnostics to troubleshoot token issues:

**1. Enable in appsettings.Development.json:**
```json
{
  "PrimusIdentity": {
    "Diagnostics": {
      "EnableInDevelopment": true,
      "IncludeTokenHints": true,
      "TrackFailures": true
    }
  }
}
```

**2. Map diagnostics endpoints:**
```csharp
if (app.Environment.IsDevelopment())
{
    app.MapPrimusDevDiagnostics();
}
```

**3. Available diagnostic endpoints:**
- `GET /_primus/diagnostics/config` — View configured issuers (sanitized)
- `GET /_primus/diagnostics/failures` — View recent auth failures
- `GET /_primus/diagnostics/failures/stats` — Failure statistics
- `POST /_primus/diagnostics/validate-token` — Test token validation
- `DELETE /_primus/diagnostics/failures` — Clear failure history

### Decoding Tokens for Debugging

Use [jwt.io](https://jwt.io) to decode tokens and verify:
1. `iss` (issuer) matches a configured `Issuer` value
2. `aud` (audience) matches a configured `Audiences` value
3. `exp` (expiration) is in the future
4. Signature is valid (for local JWT, use your secret)

**⚠️ Never paste production tokens into online decoders**

---

## 11. FAQ

### Q: Can I use multiple identity providers simultaneously?
**A:** Yes. Configure multiple entries in the `Issuers` array. The validator automatically routes tokens to the correct validator based on the `iss` claim.

### Q: Does Primus Identity store any user data?
**A:** No. All validation happens locally in your application. Primus never receives, stores, or transmits user tokens or PII.

### Q: How do I add custom claims to the user context?
**A:** Use `ClaimMappings` in your issuer configuration:
```json
{
  "ClaimMappings": {
    "https://myapp.com/tenant": "tenant_id",
    "department": "department"
  }
}
```

### Q: What's the difference between `[Authorize]` and `[PrimusAuthenticated]`?
**A:** `[PrimusAuthenticated]` is an alias for `[Authorize]` that improves discoverability. Use `[PrimusAuthorizeRoles]` or `[PrimusAuthorizePermissions]` for more granular control.

### Q: How do I support machine-to-machine (M2M) tokens?
**A:** Set `AllowMachineToMachine: true` on the issuer configuration. For Azure AD, this enables validation of tokens obtained via client_credentials flow.

### Q: How do I migrate from manual JWT Bearer configuration?
**A:** Replace `AddAuthentication().AddJwtBearer()` with `AddPrimusIdentity()`. The middleware pipeline (`UseAuthentication`, `UseAuthorization`) remains the same.

### Q: Can I use this with Azure Functions?
**A:** Yes, but you'll need to manually validate tokens using the `ITokenValidator` service rather than middleware. See the SDK source for `ITokenValidator` usage.

---

## 12. Version Compatibility

| SDK Version | .NET 6 | .NET 7 | .NET 8 | .NET 9 | Notes |
|-------------|--------|--------|--------|--------|-------|
| 1.5.0 | ✅ | ✅ | ✅ | ✅ | Current release, SourceLink enabled |
| 1.4.0 | ✅ | ✅ | ✅ | ✅ | Added .NET 9 support |
| 1.3.6 | ✅ | ✅ | ✅ | ❌ | Last .NET 8 max version |

### Breaking Changes

**v1.5.0**: No breaking changes. New features are additive.

**v1.4.0**: No breaking changes. Added net9.0 target.

### Upgrading

```bash
dotnet add package PrimusSaaS.Identity.Validator --version 1.5.0
```

---

## 13. Next Steps

After integrating Identity Validator, consider these complementary modules:

| Module | Purpose | Docs |
|--------|---------|------|
| **[Logging Module](/docs/modules/logging-module)** | Add structured logging with correlation IDs and PII masking | →Next |
| **[Notifications Module](/docs/modules/notifications)** | Send templated emails/SMS with Liquid templates | →Then |
| **[Feature Flags](/docs/modules/feature-flags)** | Control feature rollouts with percentage and user targeting | →Optional |

### Full Integration Example

See the [Live Demo API](/docs/modules/live-demo-api) for a complete working example with all modules integrated.

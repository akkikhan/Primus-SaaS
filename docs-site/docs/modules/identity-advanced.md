---
id: identity-advanced
title: Identity Validator - Advanced Features
sidebar_position: 6
description: Advanced features including PrimusAuthorize, Swagger integration, diagnostics, and telemetry.
---

# Advanced Features

Unlock the full power of Primus Identity Validator with advanced authorization attributes, Swagger UI integration, diagnostics endpoints, and observability features.

---

## PrimusAuthorize Attributes

Beyond standard `[Authorize]`, Primus provides specialized authorization attributes.

### Basic Usage

```csharp
using PrimusSaaS.Identity.Validator;

[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    // Standard authentication required
    [PrimusAuthorize]
    [HttpGet("basic")]
    public IActionResult Basic() => Ok("Authenticated");

    // Require specific role
    [PrimusAuthorize(Roles = "admin")]
    [HttpGet("admin")]
    public IActionResult AdminOnly() => Ok("Admin access");

    // Require specific policy
    [PrimusAuthorize(Policy = "CanEdit")]
    [HttpPost("edit")]
    public IActionResult Edit() => Ok("Can edit");
}
```

### Require Specific Issuer

```csharp
// Only accept Azure AD tokens
[PrimusAuthorize(Issuer = "AzureAd")]
[HttpGet("azure-only")]
public IActionResult AzureOnly()
{
    return Ok(new { message = "Azure AD authenticated" });
}

// Only accept Auth0 tokens
[PrimusAuthorize(Issuer = "Auth0")]
[HttpGet("auth0-only")]
public IActionResult Auth0Only()
{
    return Ok(new { message = "Auth0 authenticated" });
}
```

### Require Specific Scopes

```csharp
// Require read scope
[PrimusAuthorize(Scopes = "read")]
[HttpGet("data")]
public IActionResult GetData() => Ok(data);

// Require multiple scopes
[PrimusAuthorize(Scopes = "read,write")]
[HttpPost("data")]
public IActionResult PostData([FromBody] Data data) => Ok();

// Require admin scope
[PrimusAuthorize(Scopes = "admin")]
[HttpDelete("data/{id}")]
public IActionResult DeleteData(int id) => Ok();
```

### Require Specific Claims

```csharp
// Require email verified
[PrimusAuthorize(RequireClaim = "email_verified", ClaimValue = "true")]
[HttpGet("verified-users")]
public IActionResult VerifiedOnly() => Ok();

// Require specific tenant
[PrimusAuthorize(RequireClaim = "tid", ClaimValue = "your-tenant-id")]
[HttpGet("tenant-specific")]
public IActionResult TenantSpecific() => Ok();
```

### Combined Requirements

```csharp
[PrimusAuthorize(
    Issuer = "AzureAd",
    Roles = "admin",
    Scopes = "write",
    RequireClaim = "department",
    ClaimValue = "IT"
)]
[HttpPost("it-admin-action")]
public IActionResult ItAdminAction() => Ok();
```

---

## Role-Based Authorization Policies

### Configure Policies in Program.cs

```csharp
builder.Services.AddPrimusIdentity(opts => 
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts));

builder.Services.AddAuthorization(options =>
{
    // Simple role-based policies
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("admin"));
    
    options.AddPolicy("ManagerOrAdmin", policy => 
        policy.RequireRole("manager", "admin"));

    // Claim-based policies
    options.AddPolicy("VerifiedEmail", policy => 
        policy.RequireClaim("email_verified", "true"));

    // Custom policy
    options.AddPolicy("CanApprove", policy =>
        policy.RequireAssertion(context =>
        {
            var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value);
            var level = context.User.FindFirst("approval_level")?.Value;
            return roles.Contains("manager") && int.Parse(level ?? "0") >= 2;
        }));

    // Issuer-specific policy
    options.AddPolicy("AzureADOnly", policy =>
        policy.RequireAssertion(context =>
        {
            var issuer = context.User.FindFirst("iss")?.Value ?? "";
            return issuer.Contains("microsoftonline") || issuer.Contains("sts.windows.net");
        }));
});
```

### Using Policies

```csharp
[Authorize(Policy = "AdminOnly")]
[HttpGet("admin-area")]
public IActionResult AdminArea() => Ok();

[Authorize(Policy = "CanApprove")]
[HttpPost("approve/{id}")]
public IActionResult Approve(int id) => Ok();
```

---

## Swagger UI Integration

Integrate Primus Identity with Swagger UI for authenticated API testing.

### Install NuGet Packages

```bash
dotnet add package Swashbuckle.AspNetCore
```

### Configure Swagger with JWT Auth

```csharp
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(opts => 
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts));

// Configure Swagger with JWT Bearer authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "My API", 
        Version = "v1",
        Description = "API protected by Primus Identity Validator"
    });

    // Add JWT Bearer authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
```

### OAuth2 Integration (Auth0)

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    // OAuth2 with Auth0
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("https://YOUR-TENANT.auth0.com/authorize"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID" },
                    { "profile", "Profile" },
                    { "email", "Email" }
                }
            }
        }
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "openid", "profile", "email" }
        }
    });
});

// Configure OAuth2 client ID for Swagger UI
app.UseSwaggerUI(c =>
{
    c.OAuthClientId("YOUR-SWAGGER-CLIENT-ID");
    c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
    {
        { "audience", "https://your-api" }
    });
});
```

---

## Diagnostics Endpoints

Enable development diagnostics to troubleshoot authentication issues.

### Enable Diagnostics

```json
{
  "PrimusIdentity": {
    "EnableDiagnostics": true,
    "DiagnosticsPath": "/debug/identity",
    "EnableDetailedErrors": true,
    "Issuers": [...]
  }
}
```

### Built-in Diagnostics Endpoints

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(opts => 
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts);
    opts.EnableDiagnostics = builder.Environment.IsDevelopment();
});

var app = builder.Build();

// Maps diagnostics endpoints automatically when enabled
app.UsePrimusIdentityDiagnostics(); // Only in development

app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

### Available Diagnostics Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /debug/identity/config` | Shows configured issuers (no secrets) |
| `GET /debug/identity/validate` | Validates current token |
| `GET /debug/identity/claims` | Shows all claims from token |
| `GET /debug/identity/jwks/{issuer}` | Shows JWKS for issuer |

### Custom Diagnostics Controller

```csharp
[ApiController]
[Route("debug/jwt")]
public class JwtDiagnosticsController : ControllerBase
{
    private readonly IConfiguration _config;

    public JwtDiagnosticsController(IConfiguration config)
    {
        _config = config;
    }

    // Show configured issuers (safe - no secrets)
    [HttpGet("issuers")]
    public IActionResult GetIssuers()
    {
        var issuers = _config.GetSection("PrimusIdentity:Issuers")
            .GetChildren()
            .Select(i => new {
                name = i["Name"],
                type = i["Type"],
                authority = i["Authority"],
                audience = i["Audience"] ?? i["ClientId"]
            });
        
        return Ok(issuers);
    }

    // Validate and inspect current token
    [Authorize]
    [HttpGet("validate")]
    public IActionResult ValidateToken()
    {
        var issuer = User.FindFirst("iss")?.Value;
        var audience = User.FindFirst("aud")?.Value;
        var expiry = User.FindFirst("exp")?.Value;
        
        return Ok(new {
            valid = true,
            issuer,
            audience,
            expiresAt = expiry != null 
                ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiry)).ToString() 
                : null,
            claimsCount = User.Claims.Count()
        });
    }

    // Get all claims (development only)
    [Authorize]
    [HttpGet("claims")]
    public IActionResult GetClaims()
    {
        // Never expose sensitive claims in production
        var safeClaims = User.Claims
            .Where(c => !c.Type.Contains("secret", StringComparison.OrdinalIgnoreCase))
            .Select(c => new { c.Type, c.Value });
        
        return Ok(safeClaims);
    }
}
```

---

## Telemetry & Observability

### OpenTelemetry Integration

```csharp
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("PrimusSaaS.Identity")  // Primus Identity traces
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter("PrimusSaaS.Identity")  // Primus Identity metrics
            .AddOtlpExporter();
    });

builder.Services.AddPrimusIdentity(opts => 
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts);
    opts.EnableTelemetry = true;
});
```

### Custom Activity for Token Validation

```csharp
using System.Diagnostics;

public class AuthService
{
    private static readonly ActivitySource ActivitySource = 
        new("MyApp.Auth");

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        using var activity = ActivitySource.StartActivity("ValidateToken");
        activity?.SetTag("token.length", token.Length);
        
        try
        {
            var result = await _validator.ValidateAsync(token);
            activity?.SetTag("validation.success", result != null);
            activity?.SetTag("validation.issuer", result?.FindFirst("iss")?.Value);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetTag("validation.error", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
}
```

### Application Insights Integration

```csharp
builder.Services.AddApplicationInsightsTelemetry();

// Log authentication events
builder.Services.AddPrimusIdentity(opts => 
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts);
    opts.OnAuthenticationSuccess = (ctx, user) =>
    {
        var telemetry = ctx.RequestServices.GetService<TelemetryClient>();
        telemetry?.TrackEvent("Authentication.Success", new Dictionary<string, string>
        {
            { "Issuer", user.FindFirst("iss")?.Value ?? "unknown" },
            { "UserId", user.FindFirst("sub")?.Value ?? "unknown" }
        });
    };
    opts.OnAuthenticationFailed = (ctx, error) =>
    {
        var telemetry = ctx.RequestServices.GetService<TelemetryClient>();
        telemetry?.TrackEvent("Authentication.Failed", new Dictionary<string, string>
        {
            { "Error", error.Message },
            { "Path", ctx.Request.Path }
        });
    };
});
```

---

## Rate Limiting by User

```csharp
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    // Rate limit per authenticated user
    options.AddPolicy("PerUser", context =>
    {
        var userId = context.User?.FindFirst("sub")?.Value ?? "anonymous";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });
    
    // Different limits by role
    options.AddPolicy("ByRole", context =>
    {
        var isAdmin = context.User?.IsInRole("admin") ?? false;
        var userId = context.User?.FindFirst("sub")?.Value ?? "anonymous";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isAdmin ? 1000 : 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

var app = builder.Build();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
```

Usage:

```csharp
[Authorize]
[EnableRateLimiting("PerUser")]
[HttpGet("rate-limited")]
public IActionResult RateLimitedEndpoint() => Ok();
```

---

## Token Refresh Middleware

```csharp
public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;

    public TokenRefreshMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if token is about to expire
        var expClaim = context.User.FindFirst("exp")?.Value;
        if (expClaim != null)
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim));
            var remaining = expiry - DateTimeOffset.UtcNow;
            
            // Add header suggesting refresh if < 5 minutes remaining
            if (remaining.TotalMinutes < 5 && remaining.TotalMinutes > 0)
            {
                context.Response.Headers.Append("X-Token-Refresh", "recommended");
                context.Response.Headers.Append("X-Token-Expires-In", 
                    ((int)remaining.TotalSeconds).ToString());
            }
        }
        
        await _next(context);
    }
}

// Register in pipeline
app.UseAuthentication();
app.UseMiddleware<TokenRefreshMiddleware>();
app.UseAuthorization();
```

---

## Custom Token Validation

```csharp
builder.Services.AddPrimusIdentity(opts => 
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts);
    
    // Custom validation logic
    opts.CustomValidation = async (token, claims) =>
    {
        var userId = claims.FindFirst("sub")?.Value;
        
        // Check against blocklist
        var blocklist = await GetBlockedUsers();
        if (blocklist.Contains(userId))
        {
            return (false, "User is blocked");
        }
        
        // Check subscription status
        var subscriptionService = GetSubscriptionService();
        var isActive = await subscriptionService.IsActiveAsync(userId);
        if (!isActive)
        {
            return (false, "Subscription expired");
        }
        
        return (true, null);
    };
});
```

---

## Performance Optimization

### JWKS Caching

```json
{
  "PrimusIdentity": {
    "JwksCacheDuration": "01:00:00",
    "JwksRefreshInterval": "00:30:00",
    "Issuers": [...]
  }
}
```

### Distributed Cache for Token Validation

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

builder.Services.AddPrimusIdentity(opts => 
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(opts);
    opts.UseDistributedCache = true;
    opts.TokenCacheDuration = TimeSpan.FromMinutes(5);
});
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Basic setup | [Quick Start →](/docs/modules/identity-quick-start) |
| Provider-specific | [Auth0 →](/docs/modules/identity-auth0) • [Azure AD →](/docs/modules/identity-azure-ad) |
| Multi-provider setup | [Multi-Issuer →](/docs/modules/identity-multi-issuer) |
| Full API reference | [Identity Validator Reference →](/docs/modules/identity-validator) |

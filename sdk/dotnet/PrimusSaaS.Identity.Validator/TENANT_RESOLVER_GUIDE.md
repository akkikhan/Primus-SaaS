# TenantResolver Guide - PrimusSaaS.Identity.Validator

## Overview

The `TenantResolver` feature enables **multi-tenant applications** to extract tenant context from JWT token claims. This guide shows you exactly how to use it.

---

## Quick Start

### Basic Example

```csharp
using PrimusSaaS.Identity.Validator;

builder.Services.AddPrimusIdentity(options =>
{
    // Configure your issuers...
    options.Issuers.Add(new IssuerConfig { /* ... */ });
    
    // Add tenant resolver
    options.TenantResolver = claims =>
    {
        // Extract tenant ID from 'tid' claim
        var tenantId = claims.Get("tid");
        
        if (string.IsNullOrEmpty(tenantId))
            return null;
        
        return new TenantContext
        {
            TenantId = tenantId,
            Roles = new List<string>(),
            Metadata = new Dictionary<string, object>()
        };
    };
});
```

---

## TokenClaims API Reference

The `claims` parameter in `TenantResolver` is of type `TokenClaims`, which provides multiple ways to access token claims:

### Method 1: Get() - Simple Access

```csharp
options.TenantResolver = claims =>
{
    // Get claim as string
    var tenantId = claims.Get("tid");
    var email = claims.Get("email");
    var name = claims.Get("name");
    
    return new TenantContext { TenantId = tenantId ?? "default" };
};
```

### Method 2: Get<T>() - Typed Access

```csharp
options.TenantResolver = claims =>
{
    // Get claim as specific type
    var isAdmin = claims.Get<bool>("is_admin");
    var userId = claims.Get<int>("user_id");
    
    return new TenantContext 
    { 
        TenantId = claims.Get("tid") ?? "default",
        Metadata = new Dictionary<string, object>
        {
            ["isAdmin"] = isAdmin
        }
    };
};
```

### Method 3: LINQ - Advanced Queries

```csharp
options.TenantResolver = claims =>
{
    // Use LINQ to filter and transform claims
    var roles = claims
        .Where(c => c.Key.StartsWith("role_"))
        .Select(c => c.Value.ToString() ?? string.Empty)
        .ToList();
    
    var tenantId = claims.FirstOrDefault(c => c.Key == "tid")?.Value?.ToString();
    
    return new TenantContext
    {
        TenantId = tenantId ?? "default",
        Roles = roles
    };
};
```

### Method 4: All - Dictionary Access

```csharp
options.TenantResolver = claims =>
{
    // Access all claims as dictionary
    var allClaims = claims.All;
    
    if (allClaims.ContainsKey("tenant_id"))
    {
        return new TenantContext
        {
            TenantId = allClaims["tenant_id"].ToString() ?? "default"
        };
    }
    
    return null;
};
```

---

## Complete Examples

### Example 1: Azure AD Multi-Tenant

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers.Add(new IssuerConfig
    {
        Name = "AzureAD",
        Type = IssuerType.Oidc,
        Issuer = "https://login.microsoftonline.com/{tenant-id}/v2.0",
        Authority = "https://login.microsoftonline.com/{tenant-id}/v2.0",
        Audiences = new List<string> { "your-client-id" }
    });
    
    options.TenantResolver = claims =>
    {
        // Azure AD puts tenant ID in 'tid' claim
        var tenantId = claims.Get("tid");
        
        // Extract roles from 'roles' claim (array)
        var rolesJson = claims.Get("roles");
        var roles = string.IsNullOrEmpty(rolesJson) 
            ? new List<string>() 
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(rolesJson) ?? new List<string>();
        
        return new TenantContext
        {
            TenantId = tenantId ?? "default",
            Roles = roles,
            Metadata = new Dictionary<string, object>
            {
                ["oid"] = claims.Get("oid") ?? string.Empty,
                ["email"] = claims.Get("email") ?? string.Empty
            }
        };
    };
});
```

### Example 2: Custom JWT with Tenant Claim

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers.Add(new IssuerConfig
    {
        Name = "LocalAuth",
        Type = IssuerType.Jwt,
        Issuer = "https://your-api.com",
        Secret = "your-secret-key-min-32-characters-long",
        Audiences = new List<string> { "your-api-audience" }
    });
    
    options.TenantResolver = claims =>
    {
        // Custom claim structure
        var tenantId = claims.Get("tenant_id");
        var companyName = claims.Get("company_name");
        
        // Extract roles with LINQ
        var roles = claims
            .Where(c => c.Key.StartsWith("role:"))
            .Select(c => c.Key.Substring(5)) // Remove "role:" prefix
            .ToList();
        
        return new TenantContext
        {
            TenantId = tenantId ?? "default",
            Roles = roles,
            Metadata = new Dictionary<string, object>
            {
                ["companyName"] = companyName ?? "Unknown"
            }
        };
    };
});
```

### Example 3: Multi-Issuer with Fallback

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    // Azure AD
    options.Issuers.Add(new IssuerConfig { /* ... */ });
    
    // Local Auth
    options.Issuers.Add(new IssuerConfig { /* ... */ });
    
    options.TenantResolver = claims =>
    {
        // Try multiple claim names (different issuers use different names)
        var tenantId = claims.Get("tid")           // Azure AD
                    ?? claims.Get("tenant_id")     // Custom JWT
                    ?? claims.Get("organization_id") // Alternative
                    ?? "default";
        
        // Extract roles from different possible claim structures
        var roles = new List<string>();
        
        // Try array claim
        var rolesJson = claims.Get("roles");
        if (!string.IsNullOrEmpty(rolesJson))
        {
            try
            {
                roles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(rolesJson) ?? new List<string>();
            }
            catch
            {
                // Single role as string
                roles.Add(rolesJson);
            }
        }
        
        // Try individual role claims
        if (!roles.Any())
        {
            roles = claims
                .Where(c => c.Key.StartsWith("role_"))
                .Select(c => c.Value.ToString() ?? string.Empty)
                .ToList();
        }
        
        return new TenantContext
        {
            TenantId = tenantId,
            Roles = roles
        };
    };
});
```

---

## Accessing Tenant Context

Once configured, the tenant context is automatically stored in `HttpContext.Items`:

### In Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    [HttpGet("current")]
    [Authorize]
    public IActionResult GetCurrentTenant()
    {
        // Use the extension method (Recommended)
        var tenantContext = HttpContext.GetTenantContext();
        
        if (tenantContext == null)
            return NotFound("No tenant context found");
        
        return Ok(new
        {
            tenantId = tenantContext.TenantId,
            roles = tenantContext.Roles,
            metadata = tenantContext.Metadata
        });
    }
}
```

### In Middleware

```csharp
app.Use(async (context, next) =>
{
    var tenantContext = context.GetTenantContext();
    
    if (tenantContext != null)
    {
        // Use tenant context for database routing, feature flags, etc.
        Console.WriteLine($"Request from tenant: {tenantContext.TenantId}");
    }
    
    await next();
});
```

### With Dependency Injection

```csharp
public class TenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string GetCurrentTenantId()
    {
        return _httpContextAccessor.HttpContext?.GetTenantId() ?? "default";
    }
}

// Register in Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TenantService>();
```

---

## TokenClaims Helper Methods

### Contains()

```csharp
options.TenantResolver = claims =>
{
    if (!claims.Contains("tid"))
        return null; // No tenant claim present
    
    return new TenantContext { TenantId = claims.Get("tid") ?? "default" };
};
```

### Count

```csharp
options.TenantResolver = claims =>
{
    Console.WriteLine($"Token has {claims.Count} claims");
    
    return new TenantContext { TenantId = claims.Get("tid") ?? "default" };
};
```

### FirstOrDefault()

```csharp
options.TenantResolver = claims =>
{
    // Find first claim matching condition
    var tenantId = claims.FirstOrDefault(c => c.Key.Contains("tenant"));
    
    return new TenantContext { TenantId = tenantId ?? "default" };
};
```

### Where()

```csharp
options.TenantResolver = claims =>
{
    // Get all permission claims
    var permissions = claims
        .Where(c => c.Key.StartsWith("permission:"))
        .Select(c => c.Key.Substring(11))
        .ToList();
    
    return new TenantContext
    {
        TenantId = claims.Get("tid") ?? "default",
        Metadata = new Dictionary<string, object>
        {
            ["permissions"] = permissions
        }
    };
};
```

---

## Common Patterns

### Pattern 1: Null-Safe Tenant Resolution

```csharp
options.TenantResolver = claims =>
{
    var tenantId = claims.Get("tid");
    
    // Return null if no tenant found (single-tenant mode)
    if (string.IsNullOrEmpty(tenantId))
        return null;
    
    return new TenantContext { TenantId = tenantId };
};
```

### Pattern 2: Default Tenant Fallback

```csharp
options.TenantResolver = claims =>
{
    var tenantId = claims.Get("tid") ?? "default-tenant";
    
    return new TenantContext { TenantId = tenantId };
};
```

### Pattern 3: Conditional Tenant Resolution

```csharp
options.TenantResolver = claims =>
{
    var userType = claims.Get("user_type");
    
    // Only resolve tenant for enterprise users
    if (userType != "enterprise")
        return null;
    
    return new TenantContext 
    { 
        TenantId = claims.Get("tid") ?? "default" 
    };
};
```

---

## Troubleshooting

### Issue: TenantContext is always null

**Check:**
1. TenantResolver is configured
2. TenantResolver returns non-null value
3. Token contains expected claims

**Debug:**
```csharp
options.TenantResolver = claims =>
{
    Console.WriteLine($"Claims count: {claims.Count}");
    foreach (var claim in claims)
    {
        Console.WriteLine($"  {claim.Key} = {claim.Value}");
    }
    
    var tenantId = claims.Get("tid");
    Console.WriteLine($"Resolved tenant: {tenantId}");
    
    return tenantId != null 
        ? new TenantContext { TenantId = tenantId }
        : null;
};
```

### Issue: LINQ methods not working

**Solution:** Ensure you're using the latest version (1.2.1+) where `TokenClaims` implements `IEnumerable`.

**Verify:**
```csharp
// This should work in 1.2.1+
var roles = claims.Where(c => c.Key.StartsWith("role_")).ToList();
```

---

## Best Practices

1. **Always validate tenant ID exists** before creating TenantContext
2. **Use null-safe operators** when accessing claims
3. **Log tenant resolution** for debugging
4. **Cache tenant metadata** if making external calls
5. **Return null** if tenant cannot be determined (falls back to single-tenant mode)

---

## Complete Working Example

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Identity with TenantResolver
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers.Add(new IssuerConfig
    {
        Name = "AzureAD",
        Type = IssuerType.Oidc,
        Issuer = "https://login.microsoftonline.com/{tenant-id}/v2.0",
        Authority = "https://login.microsoftonline.com/{tenant-id}/v2.0",
        Audiences = new List<string> { "your-client-id" }
    });
    
    options.TenantResolver = claims =>
    {
        var tenantId = claims.Get("tid");
        
        if (string.IsNullOrEmpty(tenantId))
            return null;
        
        var roles = claims
            .Where(c => c.Key == "roles")
            .SelectMany(c => 
            {
                var json = c.Value.ToString();
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? new List<string>();
            })
            .ToList();
        
        return new TenantContext
        {
            TenantId = tenantId,
            Roles = roles,
            Metadata = new Dictionary<string, object>
            {
                ["email"] = claims.Get("email") ?? string.Empty,
                ["name"] = claims.Get("name") ?? string.Empty
            }
        };
    };
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/tenant", [Authorize] (HttpContext context) =>
{
    var tenantContext = context.GetTenantContext();
    
    return tenantContext != null
        ? Results.Ok(tenantContext)
        : Results.NotFound("No tenant context");
});

app.MapControllers();
app.Run();
```

---

## See Also

- [README.md](README.md) - Main documentation
- [PRODUCTION_DEPLOYMENT.md](PRODUCTION_DEPLOYMENT.md) - Production setup
- [ERROR_REFERENCE.md](ERROR_REFERENCE.md) - Common errors

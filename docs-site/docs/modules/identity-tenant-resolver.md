# TenantResolver Guide

Multi-tenant support for PrimusSaaS.Identity.Validator

## Overview

The `TenantResolver` feature enables **multi-tenant applications** to extract tenant context from JWT token claims automatically.

:::info Version Requirement
TenantResolver requires **PrimusSaaS.Identity.Validator 1.2.1** or later.
:::

## Quick Start

```csharp
using PrimusSaaS.Identity.Validator;

builder.Services.AddPrimusIdentity(options =>
{
    // Configure your issuers...
    options.Issuers.Add(new IssuerConfig { /* ... */ });
    
    // Add tenant resolver
    options.TenantResolver = claims =>
    {
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

## TokenClaims API

The `claims` parameter provides multiple ways to access token claims:

### Method 1: Simple Get

```csharp
options.TenantResolver = claims =>
{
    var tenantId = claims.Get("tid");
    var email = claims.Get("email");
    var name = claims.Get("name");
    
    return new TenantContext { TenantId = tenantId ?? "default" };
};
```

### Method 2: LINQ Queries

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

### Method 3: Typed Access

```csharp
options.TenantResolver = claims =>
{
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

## Complete Examples

### Azure AD Multi-Tenant

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
        var tenantId = claims.Get("tid");
        
        var rolesJson = claims.Get("roles");
        var roles = string.IsNullOrEmpty(rolesJson) 
            ? new List<string>() 
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(rolesJson) 
              ?? new List<string>();
        
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

### Custom JWT with Tenant Claim

```csharp
options.TenantResolver = claims =>
{
    var tenantId = claims.Get("tenant_id");
    var companyName = claims.Get("company_name");
    
    var roles = claims
        .Where(c => c.Key.StartsWith("role:"))
        .Select(c => c.Key.Substring(5))
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
```

## Accessing Tenant Context

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
        var tenantContext = HttpContext.Items["TenantContext"] as TenantContext;
        
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
        var context = _httpContextAccessor.HttpContext;
        var tenantContext = context?.Items["TenantContext"] as TenantContext;
        return tenantContext?.TenantId ?? "default";
    }
}

// Register in Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TenantService>();
```

## Helper Methods

### Contains()

```csharp
if (!claims.Contains("tid"))
    return null;
```

### Count

```csharp
Console.WriteLine($"Token has {claims.Count} claims");
```

### FirstOrDefault()

```csharp
var tenantId = claims.FirstOrDefault(c => c.Key.Contains("tenant"));
```

### Where()

```csharp
var permissions = claims
    .Where(c => c.Key.StartsWith("permission:"))
    .Select(c => c.Key.Substring(11))
    .ToList();
```

## Best Practices

1. **Always validate tenant ID exists** before creating TenantContext
2. **Use null-safe operators** when accessing claims
3. **Log tenant resolution** for debugging
4. **Return null** if tenant cannot be determined
5. **Cache tenant metadata** if making external calls

## Troubleshooting

### TenantContext is always null

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

## See Also

- [Identity Validator (.NET)](./identity-validator-dotnet.md)
- [Configuration Guide](./identity-configuration.md)
- [Error Reference](./identity-error-reference.md)

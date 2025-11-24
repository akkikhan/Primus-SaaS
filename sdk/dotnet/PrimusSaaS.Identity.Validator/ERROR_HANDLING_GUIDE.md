# Error Handling Guide

## Overview

This guide explains how to handle authentication and validation errors gracefully in your application using `PrimusSaaS.Identity.Validator`. It covers customizing error responses, logging failures, and handling specific exception types.

---

## Handling Authentication Failures

By default, the middleware returns a standard `401 Unauthorized` response when authentication fails. You can customize this behavior using the `JwtBearerEvents`.

### Customizing 401 Response

To return a custom JSON error response when a token is invalid:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

builder.Services.AddPrimusIdentity(options =>
{
    // ... configuration ...
})
.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            // Skip the default logic
            context.HandleResponse();

            // Write custom JSON response
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            
            var result = new
            {
                error = "unauthorized",
                message = "You are not authorized to access this resource.",
                details = context.ErrorDescription // Optional: expose details
            };
            
            await context.Response.WriteAsJsonAsync(result);
        }
    };
});
```

### Logging Authentication Errors

To log detailed error information when validation fails:

```csharp
options.Events = new JwtBearerEvents
{
    OnAuthenticationFailed = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        
        logger.LogError(context.Exception, "Authentication failed: {Message}", context.Exception.Message);
        
        return Task.CompletedTask;
    }
};
```

---

## Common Exception Types

When handling exceptions in `OnAuthenticationFailed`, you may encounter these specific types:

| Exception Type | Description | Handling Strategy |
|----------------|-------------|-------------------|
| `SecurityTokenExpiredException` | The token has expired. | Suggest token refresh or re-login. |
| `SecurityTokenInvalidSignatureException` | The signature is invalid. | Log as potential security incident. |
| `SecurityTokenInvalidAudienceException` | The audience is incorrect. | Check client configuration. |
| `SecurityTokenInvalidIssuerException` | The issuer is incorrect. | Check discovery document or config. |
| `SecurityTokenNoExpirationException` | Token has no expiration. | Reject if policy requires expiration. |
| `SecurityTokenNotYetValidException` | Token is not valid yet (nbf). | Check clock skew settings. |

### Example: Specific Error Handling

```csharp
options.Events = new JwtBearerEvents
{
    OnAuthenticationFailed = context =>
    {
        if (context.Exception is SecurityTokenExpiredException)
        {
            context.Response.Headers.Add("Token-Expired", "true");
        }
        
        return Task.CompletedTask;
    }
};
```

---

## Tenant Resolution Errors

If you are using the `TenantResolver`, errors might occur during tenant extraction.

### Handling Null Tenant

If the resolver returns `null`, the `TenantContext` will be null. You should handle this in your controllers or middleware.

```csharp
public IActionResult Get()
{
    var tenant = HttpContext.GetTenantContext();
    
    if (tenant == null)
    {
        // Decide: Return 403 Forbidden or treat as "Default" tenant?
        return StatusCode(403, "Tenant context required");
    }
    
    return Ok(tenant);
}
```

### Handling Resolver Exceptions

If your `TenantResolver` delegate throws an exception, it will bubble up. Wrap your resolver logic in a try-catch block if you want to fail gracefully.

```csharp
options.TenantResolver = claims =>
{
    try
    {
        // Complex logic that might fail
        return ResolveTenant(claims);
    }
    catch (Exception ex)
    {
        // Log error
        Console.Error.WriteLine($"Tenant resolution failed: {ex.Message}");
        return null; // Fail safe
    }
};
```

---

## Troubleshooting Guide

For a reference of specific error messages and their causes, please see [ERROR_REFERENCE.md](./ERROR_REFERENCE.md).

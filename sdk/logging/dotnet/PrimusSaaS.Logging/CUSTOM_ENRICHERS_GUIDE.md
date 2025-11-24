# Custom Enrichers Guide

## Overview

Enrichers allow you to automatically add contextual information to every log entry. This is powerful for tracking request IDs, user context, tenant information, or environment details without cluttering your business logic.

The `IEnricher` interface is simple:

```csharp
public interface IEnricher
{
    void Enrich(Dictionary<string, object> context);
}
```

---

## Creating a Custom Enricher

### Example 1: Static Property Enricher

Useful for adding environment or machine-level details.

```csharp
using PrimusSaaS.Logging.Core;

public class MachineInfoEnricher : IEnricher
{
    private readonly string _machineName;
    private readonly string _osVersion;

    public MachineInfoEnricher()
    {
        _machineName = Environment.MachineName;
        _osVersion = Environment.OSVersion.ToString();
    }

    public void Enrich(Dictionary<string, object> context)
    {
        context["MachineName"] = _machineName;
        context["OS"] = _osVersion;
    }
}
```

### Example 2: Request Context Enricher (ASP.NET Core)

Useful for adding Request ID, User ID, or Tenant ID from `HttpContext`.

**Note:** Since enrichers are singletons in the logger, you need to use `IHttpContextAccessor` to access per-request data.

```csharp
using Microsoft.AspNetCore.Http;
using PrimusSaaS.Logging.Core;

public class HttpContextEnricher : IEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(Dictionary<string, object> context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // Add Request ID
        if (httpContext.Items.TryGetValue("RequestId", out var requestId))
        {
            context["RequestId"] = requestId;
        }
        else
        {
            context["RequestId"] = httpContext.TraceIdentifier;
        }

        // Add User ID if authenticated
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            context["UserId"] = httpContext.User.Identity.Name;
        }
        
        // Add Client IP
        context["ClientIp"] = httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
```

---

## Registering Enrichers

You register enrichers during the logger configuration.

### Basic Registration

```csharp
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "PaymentService";
    
    // Add simple enricher
    options.Enrichers.Add(new MachineInfoEnricher());
});
```

### Dependency Injection with Enrichers

If your enricher needs dependencies (like `IHttpContextAccessor`), you can resolve it from the service provider *before* configuring logging, or manually construct it if dependencies are available.

However, since `AddPrimus` is often called in `Program.cs` before the container is built, a common pattern for `IHttpContextAccessor` is:

```csharp
// 1. Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 2. Configure Logging
builder.Logging.AddPrimus(options =>
{
    // Note: We can't easily inject IHttpContextAccessor here because 
    // the service provider isn't built yet.
    
    // Workaround: Use a factory or service locator pattern inside the enricher
    // OR configure logging after build (advanced)
});
```

**Recommended Pattern for ASP.NET Core:**

For ASP.NET Core, we recommend using the `Middleware` approach for request-scoped enrichment, or using a `Func<IServiceProvider, IEnricher>` if supported (future feature).

For now, you can use a static accessor or a lazy resolution inside the enricher:

```csharp
public class LazyHttpContextEnricher : IEnricher
{
    private readonly IServiceProvider _services;

    public LazyHttpContextEnricher(IServiceProvider services)
    {
        _services = services;
    }

    public void Enrich(Dictionary<string, object> context)
    {
        // Resolve accessor on demand
        var accessor = _services.GetService<IHttpContextAccessor>();
        // ... use accessor
    }
}
```

---

## Best Practices

1. **Keep it Fast**: `Enrich` is called for *every* log message. Avoid database calls, file I/O, or complex calculations.
2. **Handle Nulls**: Always check for null values (e.g., `HttpContext` might be null in background threads).
3. **Avoid Exceptions**: Wrap logic in try-catch blocks. An exception in an enricher could crash the logging pipeline.
4. **Use Unique Keys**: Prefix your keys (e.g., `ctx_userId`) to avoid collisions with standard log properties.

---

## Built-in Enrichers

PrimusSaaS Logging comes with some built-in enrichment logic:

- **Timestamp**: Added automatically (`@timestamp`).
- **Level**: Added automatically (`level`).
- **ApplicationId**: Added from options (`app_id`).
- **Environment**: Added from options (`env`).

You do not need to add these manually.

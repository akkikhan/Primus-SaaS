# Integration Patterns

This guide provides real-world examples of how to integrate `PrimusSaaS.Identity.Validator` into your application architecture.

---

## 1. Controller Integration

The most common pattern is accessing user and tenant context directly in your controllers.

### Base Controller Pattern

Create a base controller to simplify access to common properties.

```csharp
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator;

public abstract class BaseApiController : ControllerBase
{
    protected string CurrentUserId => User.Get("sub") ?? User.Get("oid") ?? string.Empty;
    protected string CurrentTenantId => HttpContext.GetTenantId() ?? "default";
    protected TenantContext? CurrentTenant => HttpContext.GetTenantContext();
    
    protected PrimusUser? CurrentUser => HttpContext.GetPrimusUser();
}

[ApiController]
[Route("api/[controller]")]
public class OrdersController : BaseApiController
{
    [HttpGet]
    public IActionResult GetOrders()
    {
        // Use properties from base controller
        var tenantId = CurrentTenantId;
        var userId = CurrentUserId;
        
        return Ok(new { tenantId, userId });
    }
}
```

---

## 2. Service Integration (Dependency Injection)

To access context in your service layer, inject `IHttpContextAccessor`.

### Tenant Service

Encapsulate tenant resolution logic in a dedicated service.

```csharp
public interface ITenantService
{
    string GetTenantId();
    TenantContext? GetTenantContext();
}

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetTenantId()
    {
        return _httpContextAccessor.HttpContext?.GetTenantId() ?? "default";
    }

    public TenantContext? GetTenantContext()
    {
        return _httpContextAccessor.HttpContext?.GetTenantContext();
    }
}

// Register in Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();
```

### Usage in Business Logic

```csharp
public class OrderService
{
    private readonly ITenantService _tenantService;
    private readonly IOrderRepository _repository;

    public OrderService(ITenantService tenantService, IOrderRepository repository)
    {
        _tenantService = tenantService;
        _repository = repository;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        var tenantId = _tenantService.GetTenantId();
        return await _repository.GetOrdersByTenantAsync(tenantId);
    }
}
```

---

## 3. Middleware Integration

You might need to validate tenant status or enforce policies in middleware.

```csharp
public class TenantValidationMiddleware
{
    private readonly RequestDelegate _next;

    public TenantValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip for public endpoints
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            await _next(context);
            return;
        }

        var tenant = context.GetTenantContext();
        
        if (tenant == null)
        {
            // Optional: Enforce tenant presence
            // context.Response.StatusCode = 403;
            // await context.Response.WriteAsync("Tenant context required");
            // return;
        }
        else
        {
            // Example: Check if tenant is active
            if (tenant.Metadata.TryGetValue("status", out var status) && status.ToString() == "suspended")
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Tenant is suspended");
                return;
            }
        }

        await _next(context);
    }
}

// Register in Program.cs
app.UseAuthentication();
app.UsePrimusIdentityValidator(); // Ensures context is set
app.UseMiddleware<TenantValidationMiddleware>();
app.UseAuthorization();
```

---

## 4. Minimal API Integration

Minimal APIs can inject `HttpContext` or `ClaimsPrincipal` directly.

```csharp
app.MapGet("/api/me", (HttpContext context, ClaimsPrincipal user) =>
{
    var tenantId = context.GetTenantId();
    var userId = user.Get("sub");
    
    return Results.Ok(new { tenantId, userId });
})
.RequireAuthorization();
```

---

## 5. Background Services

Background services (HostedServices) do not have an `HttpContext`. You must pass necessary context (TenantId, UserId) when queuing the job.

```csharp
public class JobQueue
{
    public void EnqueueJob(string tenantId, Action job)
    {
        // Store tenantId with the job payload
    }
}

// In Controller
public IActionResult StartJob([FromServices] JobQueue queue)
{
    var tenantId = HttpContext.GetTenantId();
    queue.EnqueueJob(tenantId, () => { /* ... */ });
    return Ok();
}
```

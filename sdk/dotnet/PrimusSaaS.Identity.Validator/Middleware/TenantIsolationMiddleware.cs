using Microsoft.AspNetCore.Http;

namespace PrimusSaaS.Identity.Validator.Middleware;

/// <summary>
/// Middleware that enforces tenant isolation by ensuring a valid TenantContext exists.
/// Returns 403 Forbidden if no tenant context is resolved for authenticated requests.
/// </summary>
public class TenantIsolationMiddleware
{
    private readonly RequestDelegate _next;

    public TenantIsolationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if user is not authenticated
        // We rely on UseAuthentication to have run before this
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Check for TenantContext using the extension method
        var tenantContext = context.GetTenantContext();

        if (tenantContext == null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\": \"Tenant context required\", \"message\": \"Access denied: No tenant context resolved.\"}");
            return;
        }

        await _next(context);
    }
}

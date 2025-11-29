using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PrimusSaaS.Identity.Validator.Diagnostics;

/// <summary>
/// Exposes a minimal diagnostics endpoint for authentication errors.
/// </summary>
public static class PrimusIdentityAuthDiagnosticsExtensions
{
    /// <summary>
    /// Adds a lightweight diagnostics endpoint that surfaces auth failure hints (via X-Primus-Auth-Error header).
    /// </summary>
    public static IEndpointRouteBuilder MapPrimusIdentityAuthDiagnostics(this IEndpointRouteBuilder endpoints, string pattern = "/_primus/identity/diagnose")
    {
        endpoints.MapGet(pattern, async context =>
        {
            var lastError = context.Response.Headers.TryGetValue("X-Primus-Auth-Error", out var header) ? header.ToString() : null;
            await context.Response.WriteAsJsonAsync(new
            {
                status = "ok",
                note = "Authentication errors surface in the X-Primus-Auth-Error response header.",
                lastErrorHeader = lastError
            });
        });

        return endpoints;
    }
}

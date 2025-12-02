using Microsoft.AspNetCore.Routing;

namespace PrimusSaaS.Identity.Validator.Diagnostics;

/// <summary>
/// Backward-compatible namespace shim for callers that imported PrimusSaaS.Identity.Validator.Diagnostics.
/// </summary>
public static class PrimusIdentityAuthDiagnosticsExtensionsShim
{
    public static IEndpointRouteBuilder MapPrimusIdentityAuthDiagnostics(this IEndpointRouteBuilder endpoints, string pattern = "/_primus/identity/diagnose")
        => PrimusSaaS.Identity.Validator.PrimusIdentityAuthDiagnosticsExtensions.MapPrimusIdentityAuthDiagnostics(endpoints, pattern);
}

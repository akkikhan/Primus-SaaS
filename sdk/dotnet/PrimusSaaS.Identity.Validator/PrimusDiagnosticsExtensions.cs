using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PrimusSaaS.Identity.Validator.Services;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Extensions to expose Primus Identity diagnostics via minimal APIs.
/// </summary>
public static class PrimusDiagnosticsExtensions
{
    /// <summary>
    /// Maps a GET endpoint that returns an identity diagnostics snapshot (issuers + JWKS stats).
    /// </summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <param name="pattern">Route pattern, defaults to "/primus/diagnostics".</param>
    public static IEndpointRouteBuilder MapPrimusIdentityDiagnostics(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/primus/diagnostics")
    {
        endpoints.MapGet(pattern, ([FromServices] IdentityDiagnosticsService? diagService) =>
        {
            if (diagService is null)
            {
                return Results.Json(new
                {
                    error = "Primus Identity is not configured",
                    message = "Call builder.Services.AddPrimusIdentity() to enable identity diagnostics"
                }, statusCode: 503);
            }
            var snapshot = diagService.GetSnapshot();
            return Results.Json(snapshot);
        })
        .WithDisplayName("Primus Identity Diagnostics");

        return endpoints;
    }
}

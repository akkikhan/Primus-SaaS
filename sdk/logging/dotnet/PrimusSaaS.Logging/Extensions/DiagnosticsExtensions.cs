using System.Text.Json;
using Microsoft.AspNetCore.Builder;
#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Extensions;

/// <summary>
/// Diagnostics/health endpoints for Primus logging (opt-in).
/// </summary>
public static class DiagnosticsExtensions
{
    /// <summary>
    /// Maps a lightweight health endpoint returning metrics and target info.
    /// </summary>
    public static IEndpointConventionBuilder MapPrimusLoggingHealth(this IEndpointRouteBuilder endpoints, string pattern = "/_primus/logging/health")
    {
        return endpoints.MapGet(pattern, (Logger logger) =>
        {
            var snapshot = logger.GetHealthSnapshot();
            return Results.Text(JsonSerializer.Serialize(snapshot), "application/json");
        });
    }
}
#endif

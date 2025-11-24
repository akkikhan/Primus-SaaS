using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging;

/// <summary>
/// Minimal API extensions to expose Primus logging metrics.
/// </summary>
public static class LoggingDiagnosticsExtensions
{
    /// <summary>
    /// Maps a GET endpoint that returns the current logging metrics snapshot (written/dropped/failures).
    /// </summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <param name="pattern">Route pattern, defaults to /primus/logging/metrics.</param>
    public static IEndpointRouteBuilder MapPrimusLoggingMetrics(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/primus/logging/metrics")
    {
        endpoints.MapGet(pattern, (Logger logger) =>
        {
            var metrics = logger.GetMetricsSnapshot();
            return Results.Json(metrics);
        })
        .WithDisplayName("Primus Logging Metrics");

        return endpoints;
    }
}

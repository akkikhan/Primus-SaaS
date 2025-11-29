using Microsoft.AspNetCore.Builder;
using PrimusSaaS.Logging.Middleware;

namespace PrimusSaaS.Logging.Extensions;

/// <summary>
/// Extension methods for configuring Primus Logging middleware
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Legacy wrapper for adding Primus Logging middleware.
    /// Prefer app.UsePrimusLogging() via LoggingExtensions to avoid namespace ambiguity.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    [Obsolete("Use app.UsePrimusLogging() via PrimusSaaS.Logging.Extensions.LoggingExtensions. This legacy alias will be removed in a future release.")]
    public static IApplicationBuilder UsePrimusLoggingLegacy(IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return LoggingExtensions.UsePrimusLogging(app);
    }
}

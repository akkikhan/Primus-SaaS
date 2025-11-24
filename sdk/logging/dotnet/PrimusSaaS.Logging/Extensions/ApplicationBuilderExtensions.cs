using Microsoft.AspNetCore.Builder;
using PrimusSaaS.Logging.Middleware;

namespace PrimusSaaS.Logging.Extensions;

/// <summary>
/// Extension methods for configuring Primus Logging middleware
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Primus Logging middleware to the pipeline.
    /// This middleware enriches logs with HTTP context information (User, Request ID, etc.).
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UsePrimusLogging(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app.UseMiddleware<LoggingMiddleware>();
    }
}

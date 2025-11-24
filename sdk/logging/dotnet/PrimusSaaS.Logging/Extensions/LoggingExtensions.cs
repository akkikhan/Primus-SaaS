using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Middleware;

namespace PrimusSaaS.Logging.Extensions;

/// <summary>
/// Extension methods for integrating Primus Logging with ASP.NET Core
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds Primus Logging to the service collection
    /// </summary>
    public static IServiceCollection AddPrimusLogging(
        this IServiceCollection services,
        Action<LoggerOptions> configure)
    {
        var options = new LoggerOptions();
        configure(options);

        var logger = new Logger(options);
        services.AddSingleton(logger);
        services.AddSingleton(logger.GetHealthSnapshot);
        services.AddSingleton(logger.GetMetricsSnapshot);

        return services;
    }

    /// <summary>
    /// Adds Primus Logging middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UsePrimusLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<LoggingMiddleware>();
    }
}

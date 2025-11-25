using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Health;
using PrimusSaaS.Logging.Middleware;

namespace PrimusSaaS.Logging.Extensions;

/// <summary>
/// Extension methods for integrating Primus Logging with ASP.NET Core
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds Primus Logging to the service collection with code-based configuration.
    /// </summary>
    public static IServiceCollection AddPrimusLogging(
        this IServiceCollection services,
        Action<LoggerOptions> configure)
    {
        var optionsBuilder = services.AddOptions<LoggerOptions>();
        optionsBuilder.Configure(configure);
        optionsBuilder.PostConfigure(LoggerOptionsValidator.Validate);

        RegisterLogger(services);
        return services;
    }

    /// <summary>
    /// Adds Primus Logging to the service collection and binds configuration from an IConfiguration section.
    /// </summary>
    public static IServiceCollection AddPrimusLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<LoggerOptions>? configure = null)
    {
        var optionsBuilder = services.AddOptions<LoggerOptions>();
        optionsBuilder.Bind(configuration);
        if (configure != null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.PostConfigure(LoggerOptionsValidator.Validate);

        RegisterLogger(services);
        return services;
    }

    /// <summary>
    /// Adds Primus Logging middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UsePrimusLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<LoggingMiddleware>();
    }

    private static void RegisterLogger(IServiceCollection services)
    {
        services.TryAddSingleton<Logger>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LoggerOptions>>().Value;
            return new Logger(options);
        });

        services.TryAddSingleton<Func<LoggingHealthSnapshot>>(sp =>
        {
            var logger = sp.GetRequiredService<Logger>();
            return logger.GetHealthSnapshot;
        });

        services.TryAddSingleton<Func<Core.LoggingMetricsSnapshot>>(sp =>
        {
            var logger = sp.GetRequiredService<Logger>();
            return logger.GetMetricsSnapshot;
        });
    }
}

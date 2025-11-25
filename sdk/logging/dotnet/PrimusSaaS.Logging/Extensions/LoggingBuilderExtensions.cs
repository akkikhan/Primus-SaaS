using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Health;
using PrimusSaaS.Logging.Providers;

namespace PrimusSaaS.Logging.Extensions;

/// <summary>
/// Extension methods for integrating Primus Logging with Microsoft.Extensions.Logging
/// </summary>
public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Adds Primus Logging as a provider to the logging builder with custom configuration.
    /// </summary>
    public static ILoggingBuilder AddPrimus(this ILoggingBuilder builder, Action<LoggerOptions> configure)
    {
        return builder.AddPrimusInternal(optionsBuilder => optionsBuilder.Configure(configure));
    }

    /// <summary>
    /// Adds Primus Logging with default options (ApplicationId/Environment placeholders).
    /// </summary>
    public static ILoggingBuilder AddPrimus(this ILoggingBuilder builder)
    {
        return builder.AddPrimus(options =>
        {
            options.ApplicationId = "APP";
            options.Environment = "development";
        });
    }

    /// <summary>
    /// Adds Primus Logging and binds configuration from an IConfiguration section.
    /// </summary>
    public static ILoggingBuilder AddPrimus(this ILoggingBuilder builder, IConfiguration configuration)
    {
        return builder.AddPrimus(configuration, null);
    }

    /// <summary>
    /// Adds Primus Logging, binds configuration, and allows additional code-based overrides.
    /// </summary>
    public static ILoggingBuilder AddPrimus(this ILoggingBuilder builder, IConfiguration configuration, Action<LoggerOptions>? configure)
    {
        return builder.AddPrimusInternal(optionsBuilder =>
        {
            optionsBuilder.Bind(configuration);
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }
        });
    }

    /// <summary>
    /// Alias for AddPrimus to match documentation wording.
    /// </summary>
    public static ILoggingBuilder AddPrimusLogging(this ILoggingBuilder builder, Action<LoggerOptions> configure)
    {
        return builder.AddPrimus(configure);
    }

    /// <summary>
    /// Alias for AddPrimus with default options to match documentation wording.
    /// </summary>
    public static ILoggingBuilder AddPrimusLogging(this ILoggingBuilder builder)
    {
        return builder.AddPrimus();
    }

    private static ILoggingBuilder AddPrimusInternal(this ILoggingBuilder builder, Action<OptionsBuilder<LoggerOptions>> configureOptions)
    {
        var optionsBuilder = builder.Services.AddOptions<LoggerOptions>();
        configureOptions(optionsBuilder);
        optionsBuilder.PostConfigure(LoggerOptionsValidator.Validate);

        builder.Services.TryAddSingleton<Core.Logger>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<LoggerOptions>>().Value;
            return new Core.Logger(opts);
        });

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, PrimusLoggerProvider>());

        // Expose health/metrics delegates for diagnostics consumers
        builder.Services.TryAddSingleton<Func<LoggingMetricsSnapshot>>(sp =>
        {
            var logger = sp.GetRequiredService<Core.Logger>();
            return logger.GetMetricsSnapshot;
        });

        builder.Services.TryAddSingleton<Func<LoggingHealthSnapshot>>(sp =>
        {
            var logger = sp.GetRequiredService<Core.Logger>();
            return logger.GetHealthSnapshot;
        });

        return builder;
    }
}

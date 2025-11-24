using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Providers;

namespace PrimusSaaS.Logging.Extensions;

/// <summary>
/// Extension methods for integrating Primus Logging with Microsoft.Extensions.Logging
/// </summary>
public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Adds Primus Logging as a provider to the logging builder
    /// </summary>
    public static ILoggingBuilder AddPrimus(this ILoggingBuilder builder, Action<LoggerOptions> configure)
    {
        var options = new LoggerOptions();
        configure(options);

        var primusLogger = new Core.Logger(options);

        builder.Services.AddSingleton(primusLogger);
        builder.Services.AddSingleton<ILoggerProvider>(sp => new PrimusLoggerProvider(primusLogger));

        return builder;
    }

    /// <summary>
    /// Adds Primus Logging as a provider with default options
    /// </summary>
    public static ILoggingBuilder AddPrimus(this ILoggingBuilder builder)
    {
        return builder.AddPrimus(options =>
        {
            options.ApplicationId = "APP";
            options.Environment = "development";
        });
    }
}

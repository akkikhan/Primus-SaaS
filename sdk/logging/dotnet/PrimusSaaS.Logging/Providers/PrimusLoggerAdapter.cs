using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using PrimusLogLevel = PrimusSaaS.Logging.Core.LogLevel;

namespace PrimusSaaS.Logging.Providers;

/// <summary>
/// Adapts Microsoft.Extensions.Logging.ILogger to PrimusSaaS.Logging.Core.Logger
/// </summary>
public class PrimusLoggerAdapter : ILogger
{
    private readonly string _categoryName;
    private readonly Core.Logger _primusLogger;

    public PrimusLoggerAdapter(string categoryName, Core.Logger primusLogger)
    {
        _categoryName = categoryName;
        _primusLogger = primusLogger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // Scope support could be implemented by pushing to a context stack
        // For now, we'll return null as basic implementation
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(
        LogLevel logLevel, 
        EventId eventId, 
        TState state, 
        Exception? exception, 
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var primusLevel = MapLogLevel(logLevel);

        var context = new Dictionary<string, object>
        {
            ["category"] = _categoryName,
            ["eventId"] = eventId.Id
        };

        if (eventId.Name != null)
        {
            context["eventName"] = eventId.Name;
        }

        if (exception != null)
        {
            context["exception"] = exception;
        }

        // Extract structured logging state if available
        if (state is IEnumerable<KeyValuePair<string, object>> structure)
        {
            foreach (var property in structure)
            {
                // Skip the original format placeholder
                if (property.Key != "{OriginalFormat}")
                {
                    context[property.Key] = property.Value;
                }
            }
        }

        switch (primusLevel)
        {
            case PrimusLogLevel.Debug:
                _primusLogger.Debug(message, context);
                break;
            case PrimusLogLevel.Info:
                _primusLogger.Info(message, context);
                break;
            case PrimusLogLevel.Warning:
                _primusLogger.Warn(message, context);
                break;
            case PrimusLogLevel.Error:
                _primusLogger.Error(message, context);
                break;
            case PrimusLogLevel.Critical:
                _primusLogger.Critical(message, context);
                break;
        }
    }

    private PrimusLogLevel MapLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => PrimusLogLevel.Debug,
            LogLevel.Debug => PrimusLogLevel.Debug,
            LogLevel.Information => PrimusLogLevel.Info,
            LogLevel.Warning => PrimusLogLevel.Warning,
            LogLevel.Error => PrimusLogLevel.Error,
            LogLevel.Critical => PrimusLogLevel.Critical,
            _ => PrimusLogLevel.Info
        };
    }
}

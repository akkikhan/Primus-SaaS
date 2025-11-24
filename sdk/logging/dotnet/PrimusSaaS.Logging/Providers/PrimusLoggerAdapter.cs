using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Generated;
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
    private readonly ILogger _adapterLogger;

    public PrimusLoggerAdapter(string categoryName, Core.Logger primusLogger)
    {
        _categoryName = categoryName;
        _primusLogger = primusLogger;
        _adapterLogger = LoggerFactory.Create(builder => { }).CreateLogger(categoryName);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        var scopeDictionary = ToDictionary(state);
        return _primusLogger.BeginScope(scopeDictionary);
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

        var context = new Dictionary<string, object?>
        {
            ["category"] = _categoryName,
            ["eventId"] = eventId.Id
        };

        if (eventId.Name != null)
        {
            context["eventName"] = eventId.Name;
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
                AdapterLoggingMessages.Debug(_adapterLogger, message);
                if (exception != null) _primusLogger.Debug(exception, message, context);
                else _primusLogger.Debug(message, context);
                break;
            case PrimusLogLevel.Info:
                AdapterLoggingMessages.Info(_adapterLogger, message);
                if (exception != null) _primusLogger.Info(exception, message, context);
                else _primusLogger.Info(message, context);
                break;
            case PrimusLogLevel.Warning:
                AdapterLoggingMessages.Warn(_adapterLogger, message);
                if (exception != null) _primusLogger.Warn(exception, message, context);
                else _primusLogger.Warn(message, context);
                break;
            case PrimusLogLevel.Error:
                AdapterLoggingMessages.Error(_adapterLogger, message);
                if (exception != null) _primusLogger.Error(exception, message, context);
                else _primusLogger.Error(message, context);
                break;
            case PrimusLogLevel.Critical:
                AdapterLoggingMessages.Critical(_adapterLogger, message);
                if (exception != null) _primusLogger.Critical(exception, message, context);
                else _primusLogger.Critical(message, context);
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

    private static Dictionary<string, object?> ToDictionary<TState>(TState state)
    {
        var dict = new Dictionary<string, object?>();

        if (state is IEnumerable<KeyValuePair<string, object>> structure)
        {
            foreach (var property in structure)
            {
                if (property.Key != "{OriginalFormat}")
                {
                    dict[property.Key] = property.Value;
                }
            }
        }
        else
        {
            dict["Scope"] = state?.ToString();
        }

        return dict;
    }
}

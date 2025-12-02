using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly LoggingMetrics _metrics;
    private IExternalScopeProvider? _scopeProvider;

    public PrimusLoggerAdapter(string categoryName, Core.Logger primusLogger, IExternalScopeProvider? scopeProvider = null)
    {
        _categoryName = categoryName;
        _primusLogger = primusLogger;
        _metrics = primusLogger.Metrics;
        _scopeProvider = scopeProvider;
        _adapterLogger = NullLogger.Instance;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return BeginScopeInternal(state);
    }

    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        return BeginScopeInternal(state);
    }

    private IDisposable BeginScopeInternal<TState>(TState state)
    {
        var scopeDictionary = ToDictionary(state);
        var primusScope = _primusLogger.BeginScope(scopeDictionary);

        // Also push to external scope provider so other providers can see it if needed
        var externalScope = _scopeProvider?.Push(scopeDictionary);

        return new ScopeWrapper(primusScope, externalScope);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        var primusLevel = MapLogLevel(logLevel);
        return _primusLogger.IsEnabled(primusLevel);
    }

    public void Log<TState>(
        LogLevel logLevel, 
        EventId eventId, 
        TState state, 
        Exception? exception, 
        Func<TState, Exception?, string> formatter)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));

        var primusLevel = MapLogLevel(logLevel);
        if (!_primusLogger.IsEnabled(primusLevel)) return;

        var message = formatter(state, exception);
        var category = _categoryName;
        var options = _primusLogger.Options;

        if (options.TruncateCategoryNames && options.MaxCategoryLength > 0 && category.Length > options.MaxCategoryLength)
        {
            category = $"{category.Substring(0, options.MaxCategoryLength)}...";
        }

        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["category"] = category,
        };

        if (!ReferenceEquals(category, _categoryName) && !string.Equals(category, _categoryName, StringComparison.Ordinal))
        {
            context["categoryFull"] = _categoryName;
        }

        if (eventId.Id != 0) context["eventId"] = eventId.Id;
        if (!string.IsNullOrWhiteSpace(eventId.Name)) context["eventName"] = eventId.Name;

        var stateContext = ToDictionary(state);
        MergeContext(context, stateContext);
        MergeExternalScopes(context);

        _metrics.IncrementAdapterForwarded();

        // Log to both the adapter (for consumers that expect Microsoft logging) and Primus Logger
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

    internal void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    private void MergeExternalScopes(Dictionary<string, object?> context)
    {
        if (_scopeProvider == null) return;

        var scopes = new List<Dictionary<string, object?>>();
        _scopeProvider.ForEachScope((scopeObject, state) =>
        {
            var scopeDict = ToDictionary(scopeObject);
            if (scopeDict.Count > 0)
            {
                state.Add(scopeDict);
            }
        }, scopes);

        // Apply outermost first so inner scopes can override keys
        for (var i = scopes.Count - 1; i >= 0; i--)
        {
            MergeContext(context, scopes[i]);
        }
    }

    private static void MergeContext(Dictionary<string, object?> target, Dictionary<string, object?> source)
    {
        foreach (var kvp in source)
        {
            target[kvp.Key] = kvp.Value;
        }
    }

    private sealed class ScopeWrapper : IDisposable
    {
        private readonly IDisposable? _primusScope;
        private readonly IDisposable? _externalScope;
        private bool _disposed;

        public ScopeWrapper(IDisposable? primusScope, IDisposable? externalScope)
        {
            _primusScope = primusScope;
            _externalScope = externalScope;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _primusScope?.Dispose();
            _externalScope?.Dispose();
        }
    }
}

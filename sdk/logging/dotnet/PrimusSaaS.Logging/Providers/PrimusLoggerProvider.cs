using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PrimusSaaS.Logging.Providers;

/// <summary>
/// Provider for Primus Logger
/// </summary>
public class PrimusLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly Core.Logger _primusLogger;
    private readonly ConcurrentDictionary<string, PrimusLoggerAdapter> _loggers = new();
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public PrimusLoggerProvider(Core.Logger primusLogger)
    {
        _primusLogger = primusLogger;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new PrimusLoggerAdapter(name, _primusLogger, _scopeProvider));
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider ?? new LoggerExternalScopeProvider();

        foreach (var adapter in _loggers.Values)
        {
            adapter.SetScopeProvider(_scopeProvider);
        }
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

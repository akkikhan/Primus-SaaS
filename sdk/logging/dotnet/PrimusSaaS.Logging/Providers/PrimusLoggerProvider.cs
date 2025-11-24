using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PrimusSaaS.Logging.Providers;

/// <summary>
/// Provider for Primus Logger
/// </summary>
public class PrimusLoggerProvider : ILoggerProvider
{
    private readonly Core.Logger _primusLogger;
    private readonly ConcurrentDictionary<string, PrimusLoggerAdapter> _loggers = new();

    public PrimusLoggerProvider(Core.Logger primusLogger)
    {
        _primusLogger = primusLogger;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new PrimusLoggerAdapter(name, _primusLogger));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

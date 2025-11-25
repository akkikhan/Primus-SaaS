using PrimusSaaS.Logging.Core;
using Serilog;
using Serilog.Events;

namespace PrimusSaaS.Logging.Targets;

/// <summary>
/// Forwards Primus log entries to a Serilog pipeline (and its configured sinks).
/// </summary>
public class SerilogTarget : ITarget
{
    private readonly ILogger _logger;

    public SerilogTarget(ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;
    }

    public void Write(LogEntry logEntry)
    {
        var level = MapLevel(logEntry.Level);
        var serilogLogger = _logger;

        // Apply context as properties
        foreach (var kvp in logEntry.Context)
        {
            serilogLogger = serilogLogger.ForContext(kvp.Key, kvp.Value, destructureObjects: true);
        }

        serilogLogger.Write(level, logEntry.Message);
    }

    public void Close()
    {
        // No-op; Serilog pipeline is managed externally.
    }

    private static LogEventLevel MapLevel(Core.LogLevel level) => level switch
    {
        Core.LogLevel.Debug => LogEventLevel.Debug,
        Core.LogLevel.Info => LogEventLevel.Information,
        Core.LogLevel.Warning => LogEventLevel.Warning,
        Core.LogLevel.Error => LogEventLevel.Error,
        Core.LogLevel.Critical => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };
}

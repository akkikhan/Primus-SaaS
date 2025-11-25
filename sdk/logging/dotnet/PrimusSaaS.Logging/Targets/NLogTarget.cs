using PrimusSaaS.Logging.Core;
using CoreLogLevel = PrimusSaaS.Logging.Core.LogLevel;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLogLogger = NLog.Logger;
using NLogLogLevel = NLog.LogLevel;

namespace PrimusSaaS.Logging.Targets;

/// <summary>
/// Forwards Primus log entries into NLog (and its configured targets).
/// </summary>
public class NLogTarget : ITarget
{
    private readonly NLogLogger _logger;

    public NLogTarget(NLogLogger? logger = null)
    {
        _logger = logger ?? LogManager.GetLogger("Primus");
    }

    public void Write(LogEntry logEntry)
    {
        var evt = new LogEventInfo
        {
            Level = MapLevel(logEntry.Level),
            Message = logEntry.Message,
            TimeStamp = logEntry.Timestamp.ToLocalTime()
        };

        foreach (var kvp in logEntry.Context)
        {
            evt.Properties[kvp.Key] = kvp.Value;
        }

        _logger.Log(evt);
    }

    public void Close()
    {
        // NLog targets are managed via configuration; nothing to close here.
    }

    private static NLogLogLevel MapLevel(CoreLogLevel level) => level switch
    {
        CoreLogLevel.Debug => NLogLogLevel.Debug,
        CoreLogLevel.Info => NLogLogLevel.Info,
        CoreLogLevel.Warning => NLogLogLevel.Warn,
        CoreLogLevel.Error => NLogLogLevel.Error,
        CoreLogLevel.Critical => NLogLogLevel.Fatal,
        _ => NLogLogLevel.Info
    };
}

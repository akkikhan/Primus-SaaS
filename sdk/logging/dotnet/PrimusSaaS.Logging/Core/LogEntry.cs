using System.Text.Json;

namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Represents a single log entry
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Timestamp when the log was created
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Log severity level
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Log message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Creates a new log entry
    /// </summary>
    public static LogEntry Create(LogLevel level, string message, Dictionary<string, object>? context = null)
    {
        return new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            Context = context ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Converts the log entry to JSON
    /// </summary>
    public string ToJson()
    {
        var data = new Dictionary<string, object>
        {
            ["timestamp"] = Timestamp.ToString("O"),
            ["level"] = Level.ToString().ToUpperInvariant(),
            ["message"] = Message,
            ["context"] = Context
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}

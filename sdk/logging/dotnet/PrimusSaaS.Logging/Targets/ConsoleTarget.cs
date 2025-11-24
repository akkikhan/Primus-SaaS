using System.Text.Json;
using System.Text.Json.Serialization;
using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Targets;

/// <summary>
/// Console output target with optional pretty printing
/// </summary>
public class ConsoleTarget : ITarget
{
    private readonly bool _pretty;

    public ConsoleTarget(bool pretty = false)
    {
        _pretty = pretty;
    }

    public void Write(LogEntry logEntry)
    {
        if (_pretty)
        {
            PrettyPrint(logEntry);
        }
        else
        {
            Console.WriteLine(logEntry.ToJson());
        }
    }

    private void PrettyPrint(LogEntry logEntry)
    {
        var color = GetColor(logEntry.Level);
        var timestamp = logEntry.Timestamp.ToLocalTime().ToString("hh:mm:ss tt");
        
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"[{timestamp}] ");
        
        Console.ForegroundColor = color;
        Console.Write($"{logEntry.Level.ToString().ToUpperInvariant()}: ");
        
        Console.ResetColor();
        Console.WriteLine(logEntry.Message);

        if (logEntry.Context.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(JsonSerializer.Serialize(logEntry.Context, PrettyOptions));
            Console.ResetColor();
        }
    }

    private ConsoleColor GetColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => ConsoleColor.Cyan,
            LogLevel.Info => ConsoleColor.Green,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.Magenta,
            _ => ConsoleColor.White
        };
    }

    public void Close()
    {
        // Nothing to close for console
    }

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
}

using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Targets;

/// <summary>
/// File output target
/// </summary>
public class FileTarget : ITarget
{
    private readonly string _filePath;
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public FileTarget(string filePath)
    {
        _filePath = filePath;
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create or append to file
        _writer = new StreamWriter(filePath, append: true)
        {
            AutoFlush = true
        };
    }

    public void Write(LogEntry logEntry)
    {
        lock (_lock)
        {
            _writer.WriteLine(logEntry.ToJson());
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Close();
            _writer?.Dispose();
        }
    }
}

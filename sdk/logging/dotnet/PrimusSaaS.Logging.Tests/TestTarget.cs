using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;

namespace PrimusSaaS.Logging.Tests;

public class TestTarget : ITarget
{
    public List<LogEntry> Logs { get; } = new();
    public bool IsClosed { get; private set; }

    public void Write(LogEntry logEntry)
    {
        lock (Logs)
        {
            Logs.Add(logEntry);
        }
    }

    public void Close()
    {
        IsClosed = true;
    }
}

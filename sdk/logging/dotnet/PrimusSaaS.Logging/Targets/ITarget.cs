using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Targets;

/// <summary>
/// Interface for log output targets
/// </summary>
public interface ITarget
{
    /// <summary>
    /// Write a log entry to the target
    /// </summary>
    void Write(LogEntry logEntry);

    /// <summary>
    /// Close the target and release resources
    /// </summary>
    void Close();
}

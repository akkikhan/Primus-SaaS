namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Log severity levels
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Detailed diagnostic information
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Informational messages
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning messages
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error messages
    /// </summary>
    Error = 3,

    /// <summary>
    /// Critical failures
    /// </summary>
    Critical = 4
}

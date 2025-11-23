namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Configuration options for the logger
/// </summary>
public class LoggerOptions
{
    /// <summary>
    /// Application identifier
    /// </summary>
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>
    /// Environment (development, testing, production)
    /// </summary>
    public string Environment { get; set; } = "development";

    /// <summary>
    /// Minimum log level to output
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Info;

    /// <summary>
    /// Output targets configuration
    /// </summary>
    public List<TargetConfig> Targets { get; set; } = new() { new TargetConfig { Type = "console" } };
}

/// <summary>
/// Configuration for a log target
/// </summary>
public class TargetConfig
{
    /// <summary>
    /// Target type (console, file, applicationInsights)
    /// </summary>
    public string Type { get; set; } = "console";

    /// <summary>
    /// File path (for file target)
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Pretty print (for console target)
    /// </summary>
    public bool Pretty { get; set; }

    /// <summary>
    /// Application Insights connection string
    /// </summary>
    public string? ConnectionString { get; set; }
}

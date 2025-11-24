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

    /// <summary>
    /// PII Masking configuration
    /// </summary>
    public PiiOptions Pii { get; set; } = new();

    /// <summary>
    /// Custom enrichers to apply to every log entry
    /// </summary>
    public List<IEnricher> Enrichers { get; set; } = new();
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
    /// Format string (for compatibility with documentation).
    /// Set to "PrettyPrint" to enable Pretty printing.
    /// </summary>
    public string? Format
    {
        get => Pretty ? "PrettyPrint" : "Json";
        set
        {
            if (string.Equals(value, "PrettyPrint", StringComparison.OrdinalIgnoreCase))
            {
                Pretty = true;
            }
        }
    }

    /// <summary>
    /// Application Insights connection string
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Max file size in bytes before rotation (default: 10MB)
    /// </summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Max number of rotated files to keep (default: 5)
    /// </summary>
    public int MaxRetainedFiles { get; set; } = 5;

    /// <summary>
    /// Whether to compress rotated files (gzip)
    /// </summary>
    public bool CompressRotatedFiles { get; set; } = false;

    /// <summary>
    /// Enable asynchronous logging with buffering
    /// </summary>
    public bool Async { get; set; } = false;

    /// <summary>
    /// Buffer size for async logging (default: 1000)
    /// </summary>
    public int BufferSize { get; set; } = 1000;


}

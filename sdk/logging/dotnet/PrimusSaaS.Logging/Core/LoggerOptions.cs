using PrimusSaaS.Logging.Targets;

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
    /// Serialization safety configuration
    /// </summary>
    public SerializationOptions Serialization { get; set; } = new();

    /// <summary>
    /// Custom enrichers to apply to every log entry
    /// </summary>
    public List<IEnricher> Enrichers { get; set; } = new();

    /// <summary>
    /// Optional external metrics collector (for observability/health).
    /// If null, an internal instance will be created.
    /// </summary>
    public LoggingMetrics? Metrics { get; set; }

    /// <summary>
    /// Optional custom targets (primarily for advanced scenarios and testing).
    /// When provided, these are used instead of the built-in target factory.
    /// </summary>
    public List<ITarget>? CustomTargets { get; set; }

    /// <summary>
    /// OpenTelemetry-related enrichment settings.
    /// </summary>
    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();

    /// <summary>
    /// Client-side sampling controls to reduce log volume.
    /// </summary>
    public SamplingOptions Sampling { get; set; } = new();

    /// <summary>
    /// Convenience toggle to disable category truncation; when true, categories longer than MaxCategoryLength will be trimmed.
    /// </summary>
    public bool TruncateCategoryNames { get; set; }

    /// <summary>
    /// Maximum category length when TruncateCategoryNames is enabled.
    /// </summary>
    public int MaxCategoryLength { get; set; } = 120;

    /// <summary>
    /// Convenience setter to enable sampling with a single property (0.0 - 1.0). Values &lt; 1 enable sampling automatically.
    /// </summary>
    public double SamplingRate
    {
        get => Sampling.SampleRate;
        set
        {
            Sampling.Enabled = value < 1.0;
            Sampling.SampleRate = value;
        }
    }

    /// <summary>
    /// Ensure error/critical events bypass sampling when true.
    /// </summary>
    public bool AlwaysLogOnError
    {
        get => Sampling.AlwaysLogOnError;
        set => Sampling.AlwaysLogOnError = value;
    }

    /// <summary>
    /// Explicit sensitive field names to mask in log context (forwarded to PiiOptions.CustomSensitiveKeys).
    /// </summary>
    public List<string> MaskFields
    {
        get
        {
            Pii ??= new PiiOptions();
            return Pii.CustomSensitiveKeys;
        }
        set
        {
            Pii ??= new PiiOptions();
            Pii.CustomSensitiveKeys = value ?? new List<string>();
        }
    }

    /// <summary>
    /// Convenience Application Insights preset for config-first enablement.
    /// </summary>
    public ApplicationInsightsOptions ApplicationInsights { get; set; } = new();
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

/// <summary>
/// OpenTelemetry enrichment options.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// When true, attaches trace/span identifiers from Activity.Current onto log context.
    /// </summary>
    public bool IncludeTraceContext { get; set; } = true;
}

/// <summary>
/// Log sampling configuration.
/// </summary>
public class SamplingOptions
{
    /// <summary>
    /// Enable client-side probabilistic sampling.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Probability between 0.0 and 1.0 (e.g., 0.1 = 10% of logs kept).
    /// </summary>
    public double SampleRate { get; set; } = 1.0;

    /// <summary>
    /// When true, skip sampling for Error and Critical logs.
    /// </summary>
    public bool AlwaysLogOnError { get; set; } = true;
}

/// <summary>
/// Application Insights convenience configuration.
/// </summary>
public class ApplicationInsightsOptions
{
    /// <summary>
    /// Enable AI target without manually defining a target entry.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Connection string used by the built-in preset.
    /// </summary>
    public string? ConnectionString { get; set; }
}

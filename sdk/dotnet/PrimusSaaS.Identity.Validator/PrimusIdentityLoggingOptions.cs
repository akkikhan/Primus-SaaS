using Microsoft.Extensions.Logging;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Controls logging verbosity and redaction for Primus Identity.
/// </summary>
public class PrimusIdentityLoggingOptions
{
    /// <summary>
    /// Minimum log level for identity events. Default: Information.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// When true, suppresses token values and sensitive claim contents in logs.
    /// </summary>
    public bool RedactSensitiveData { get; set; } = true;

    /// <summary>
    /// Whether to log validation steps (issuer/audience/keys). Default: true.
    /// </summary>
    public bool LogValidationSteps { get; set; } = true;

    /// <summary>
    /// Whether to log claim mapping outcomes. Default: false to reduce noise.
    /// </summary>
    public bool LogClaimMapping { get; set; }
}

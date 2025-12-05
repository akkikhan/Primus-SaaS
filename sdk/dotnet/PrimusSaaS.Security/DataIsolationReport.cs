namespace PrimusSaaS.Security;

/// <summary>
/// Data isolation verification report.
/// </summary>
public class DataIsolationReport
{
    /// <summary>
    /// Gets or sets the timestamp of the report.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the module version.
    /// </summary>
    public string ModuleVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of verification checks.
    /// </summary>
    public List<VerificationCheck> Checks { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the module is fully isolated.
    /// </summary>
    public bool IsFullyIsolated { get; set; }
}

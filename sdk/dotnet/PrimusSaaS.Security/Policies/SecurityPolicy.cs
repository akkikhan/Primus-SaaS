using PrimusSaaS.Security.Core;

namespace PrimusSaaS.Security.Policies;

/// <summary>
/// Defines the security rules and thresholds for the application.
/// </summary>
public class SecurityPolicy
{
    /// <summary>
    /// Gets or sets the action to take for Critical severity findings.
    /// Default: Block.
    /// </summary>
    public PolicyAction CriticalSeverityAction { get; set; } = PolicyAction.Block;

    /// <summary>
    /// Gets or sets the action to take for High severity findings.
    /// Default: Block.
    /// </summary>
    public PolicyAction HighSeverityAction { get; set; } = PolicyAction.Block;

    /// <summary>
    /// Gets or sets the action to take for Medium severity findings.
    /// Default: Warn.
    /// </summary>
    public PolicyAction MediumSeverityAction { get; set; } = PolicyAction.Warn;

    /// <summary>
    /// Gets or sets the action to take for Low severity findings.
    /// Default: Audit.
    /// </summary>
    public PolicyAction LowSeverityAction { get; set; } = PolicyAction.Audit;

    /// <summary>
    /// List of specific Rule IDs or CVEs to ignore (e.g., "CVE-2023-1234", "PS0001").
    /// </summary>
    public HashSet<string> IgnoredRules { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// List of specific Packages to ignore (e.g., "LegacyPackage").
    /// Use with caution.
    /// </summary>
    public HashSet<string> IgnoredPackages { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// If true, enforces strict OWASP Top 10 compliance checks.
    /// </summary>
    public bool EnforceOwaspCompliance { get; set; } = true;
}

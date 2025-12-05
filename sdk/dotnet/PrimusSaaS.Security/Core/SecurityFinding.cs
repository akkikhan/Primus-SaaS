namespace PrimusSaaS.Security.Core;

/// <summary>
/// Represents a security finding from analysis.
/// </summary>
public class SecurityFinding
{
    /// <summary>
    /// Gets or sets the unique identifier for this finding.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the severity level of the finding.
    /// </summary>
    public SecuritySeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the title/summary of the security issue.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed description of the vulnerability.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path where the issue was found.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number in the file (1-indexed).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the column number (optional).
    /// </summary>
    public int? Column { get; set; }

    /// <summary>
    /// Gets or sets the code snippet showing the issue (NO actual secrets/PII).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recommended remediation.
    /// </summary>
    public string Remediation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CWE identifier (e.g., "CWE-89").
    /// </summary>
    public string? CWE { get; set; }

    /// <summary>
    /// Gets or sets the OWASP category (e.g., "A03:2021 - Injection").
    /// </summary>
    public string? OWASP { get; set; }

    /// <summary>
    /// Gets or sets the CVE identifier (for dependency vulnerabilities).
    /// </summary>
    public string? CVE { get; set; }

    /// <summary>
    /// Gets or sets the CVSS score (0.0 to 10.0).
    /// </summary>
    public double? CVSSScore { get; set; }

    /// <summary>
    /// Gets or sets the package name (for dependency vulnerabilities).
    /// </summary>
    public string? Package { get; set; }

    /// <summary>
    /// Gets or sets the current version (for dependency vulnerabilities).
    /// </summary>
    public string? CurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets the patched version (for dependency vulnerabilities).
    /// </summary>
    public string? PatchedVersion { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this finding was discovered.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the rule ID that detected this finding.
    /// </summary>
    public string? RuleId { get; set; }
}



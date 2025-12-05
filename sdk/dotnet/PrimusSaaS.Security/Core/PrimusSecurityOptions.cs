namespace PrimusSaaS.Security.Core;

/// <summary>
/// Configuration options for Primus Security Module.
/// </summary>
public class PrimusSecurityOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable static code analysis.
    /// </summary>
    public bool EnableStaticAnalysis { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable dependency vulnerability scanning.
    /// </summary>
    public bool EnableDependencyScanning { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable secret detection.
    /// </summary>
    public bool EnableSecretDetection { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable security policy validation.
    /// </summary>
    public bool EnablePolicyValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the compliance standards to validate against.
    /// </summary>
    public string[] ComplianceStandards { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the path to local data directory (CVE database, policies, etc.).
    /// </summary>
    public string DataPath { get; set; } = "./SecurityData";

    /// <summary>
    /// Gets or sets the path to security findings storage.
    /// </summary>
    public string FindingsPath { get; set; } = "./SecurityFindings";

    /// <summary>
    /// Gets or sets the path to generated reports.
    /// </summary>
    public string ReportsPath { get; set; } = "./SecurityReports";

    /// <summary>
    /// Gets or sets the path to CVE database file.
    /// </summary>
    public string CveDatabasePath { get; set; } = "./SecurityData/cve-database.db";

    /// <summary>
    /// Gets or sets the path to security policies directory.
    /// </summary>
    public string PoliciesPath { get; set; } = "./SecurityData/Policies";

    /// <summary>
    /// Gets or sets a value indicating whether to fail scan if critical vulnerabilities are found.
    /// </summary>
    public bool FailOnCritical { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to integrate with Primus.Logging module.
    /// </summary>
    public bool IntegrateWithLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to integrate with Primus.Notifications module.
    /// </summary>
    public bool IntegrateWithNotifications { get; set; } = false;

    /// <summary>
    /// Gets or sets the callback when critical vulnerability is found.
    /// </summary>
    public Func<SecurityFinding, Task>? OnCriticalVulnerability { get; set; }

    /// <summary>
    /// Gets or sets the callback when scan completes.
    /// </summary>
    public Func<ScanResult, Task>? OnScanComplete { get; set; }
}

namespace PrimusSaaS.Security.Core;

/// <summary>
/// Result of a security scan.
/// </summary>
public class ScanResult
{
    /// <summary>
    /// Gets or sets the unique identifier for this scan.
    /// </summary>
    public string ScanId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the time when the scan started.
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the time when the scan completed.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets the scan duration.
    /// </summary>
    public TimeSpan Duration => this.EndTime.HasValue ? this.EndTime.Value - this.StartTime : TimeSpan.Zero;

    /// <summary>
    /// Gets or sets all security findings from this scan.
    /// </summary>
    public List<SecurityFinding> Findings { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of files scanned.
    /// </summary>
    public int FilesScanned { get; set; }

    /// <summary>
    /// Gets the statistics by severity.
    /// </summary>
    public Dictionary<SecuritySeverity, int> FindingsBySeverity
    {
        get
        {
            return this.Findings
                .GroupBy(f => f.Severity)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the scan passed all checks.
    /// </summary>
    public bool Passed { get; set; } = true;

    /// <summary>
    /// Gets or sets any errors encountered during scanning.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

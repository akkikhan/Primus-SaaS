namespace PrimusSaaS.Security.Core;

/// <summary>
/// Security severity levels.
/// </summary>
public enum SecuritySeverity
{
    /// <summary>
    /// Informational - no immediate action required.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Low severity - should be addressed eventually.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium severity - should be addressed soon.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High severity - should be addressed quickly.
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical severity - requires immediate attention.
    /// </summary>
    Critical = 4,
}

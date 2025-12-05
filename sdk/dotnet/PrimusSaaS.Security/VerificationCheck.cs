namespace PrimusSaaS.Security;

/// <summary>
/// Individual verification check.
/// </summary>
public class VerificationCheck
{
    /// <summary>
    /// Gets or sets the name of the check.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the check.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the check passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets or sets the details of the check result.
    /// </summary>
    public string Details { get; set; } = string.Empty;
}

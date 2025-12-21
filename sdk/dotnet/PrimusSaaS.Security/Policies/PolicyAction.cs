namespace PrimusSaaS.Security.Policies;

/// <summary>
/// Defines the action to take when a security policy is violated.
/// </summary>
public enum PolicyAction
{
    /// <summary>
    /// Log the violation but do not stop the process.
    /// </summary>
    Audit = 0,

    /// <summary>
    /// Emit a warning but allow the process to continue.
    /// </summary>
    Warn = 1,

    /// <summary>
    /// Stop the process immediately (fail build/deployment).
    /// </summary>
    Block = 2,

    /// <summary>
    /// Explicitly ignore this type of violation.
    /// </summary>
    Ignore = 3
}

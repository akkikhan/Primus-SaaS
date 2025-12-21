using PrimusSaaS.Security.Core;

namespace PrimusSaaS.Security.Policies;

/// <summary>
/// Represents a violation of a security policy.
/// </summary>
public class PolicyViolation
{
    /// <summary>
    /// The original security finding that triggered this violation.
    /// </summary>
    public SecurityFinding Finding { get; }

    /// <summary>
    /// The action determined by the policy (Block, Warn, etc.).
    /// </summary>
    public PolicyAction Action { get; }

    /// <summary>
    /// Explanation of why this action was taken.
    /// </summary>
    public string Reason { get; }

    public PolicyViolation(SecurityFinding finding, PolicyAction action, string reason)
    {
        Finding = finding;
        Action = action;
        Reason = reason;
    }
}

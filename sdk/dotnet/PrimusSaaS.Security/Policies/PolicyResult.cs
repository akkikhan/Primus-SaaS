using System.Text;

namespace PrimusSaaS.Security.Policies;

/// <summary>
/// The result of evaluating findings against a security policy.
/// </summary>
public class PolicyResult
{
    public List<PolicyViolation> Violations { get; } = new();
    
    public bool IsSuccess => !Violations.Any(v => v.Action == PolicyAction.Block);
    
    public int BlockCount => Violations.Count(v => v.Action == PolicyAction.Block);
    public int WarnCount => Violations.Count(v => v.Action == PolicyAction.Warn);
    public int AuditCount => Violations.Count(v => v.Action == PolicyAction.Audit);

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Policy Evaluation: {(IsSuccess ? "PASSED" : "FAILED")}");
        sb.AppendLine($"Blocks: {BlockCount}, Warnings: {WarnCount}, Audits: {AuditCount}");
        
        foreach (var v in Violations.Where(v => v.Action == PolicyAction.Block))
        {
            sb.AppendLine($"[BLOCK] {v.Finding.Title}: {v.Reason}");
        }
        
        return sb.ToString();
    }
}

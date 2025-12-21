using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.Core;

namespace PrimusSaaS.Security.Policies;

public class PolicyEngine
{
    private readonly ILogger<PolicyEngine> _logger;

    public PolicyEngine(ILogger<PolicyEngine> logger)
    {
        _logger = logger;
    }

    public PolicyResult Evaluate(IEnumerable<SecurityFinding> findings, SecurityPolicy policy)
    {
        var result = new PolicyResult();

        foreach (var finding in findings)
        {
            // 1. Check Ignored Rules/CVEs
            if (!string.IsNullOrEmpty(finding.RuleId) && policy.IgnoredRules.Contains(finding.RuleId))
            {
                result.Violations.Add(new PolicyViolation(finding, PolicyAction.Ignore, "Rule explicitly ignored via policy."));
                continue;
            }

            if (!string.IsNullOrEmpty(finding.CVE) && policy.IgnoredRules.Contains(finding.CVE))
            {
                result.Violations.Add(new PolicyViolation(finding, PolicyAction.Ignore, "CVE explicitly ignored via policy."));
                continue;
            }

            // 2. Check Ignored Packages
            if (!string.IsNullOrEmpty(finding.Package) && policy.IgnoredPackages.Contains(finding.Package))
            {
                result.Violations.Add(new PolicyViolation(finding, PolicyAction.Ignore, "Package explicitly ignored via policy."));
                continue;
            }

            // 3. Determine Action based on Severity
            var action = GetActionForSeverity(finding.Severity, policy);

            // 4. Create Violation
            var reason = $"Severity {finding.Severity} triggers {action} action.";
            result.Violations.Add(new PolicyViolation(finding, action, reason));
            
            if (action == PolicyAction.Block)
            {
                _logger.LogError("Policy Violation [BLOCK]: {Title} ({Severity}) - {Reason}", finding.Title, finding.Severity, reason);
            }
            else if (action == PolicyAction.Warn)
            {
                _logger.LogWarning("Policy Violation [WARN]: {Title} ({Severity}) - {Reason}", finding.Title, finding.Severity, reason);
            }
        }

        return result;
    }

    private PolicyAction GetActionForSeverity(SecuritySeverity severity, SecurityPolicy policy)
    {
        return severity switch
        {
            SecuritySeverity.Critical => policy.CriticalSeverityAction,
            SecuritySeverity.High => policy.HighSeverityAction,
            SecuritySeverity.Medium => policy.MediumSeverityAction,
            SecuritySeverity.Low => policy.LowSeverityAction,
            SecuritySeverity.Info => PolicyAction.Audit,
            _ => PolicyAction.Audit
        };
    }
}

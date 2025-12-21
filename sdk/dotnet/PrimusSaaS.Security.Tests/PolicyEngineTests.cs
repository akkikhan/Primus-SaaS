using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PrimusSaaS.Security.Core;
using PrimusSaaS.Security.Policies;
using Xunit;

namespace PrimusSaaS.Security.Tests;

public class PolicyEngineTests
{
    private readonly Mock<ILogger<PolicyEngine>> _loggerMock = new();
    private readonly PolicyEngine _engine;

    public PolicyEngineTests()
    {
        _engine = new PolicyEngine(_loggerMock.Object);
    }

    [Fact]
    public void Evaluate_ShouldBlock_WhenCriticalSeverity_AndPolicyIsBlock()
    {
        // Arrange
        var findings = new List<SecurityFinding>
        {
            new SecurityFinding { Severity = SecuritySeverity.Critical, Title = "Critical Issue" }
        };
        var policy = new SecurityPolicy { CriticalSeverityAction = PolicyAction.Block };

        // Act
        var result = _engine.Evaluate(findings, policy);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.BlockCount.Should().Be(1);
        result.Violations.Should().ContainSingle(v => v.Action == PolicyAction.Block);
    }

    [Fact]
    public void Evaluate_ShouldPass_WhenCriticalSeverity_AndPolicyIsWarn()
    {
        // Arrange
        var findings = new List<SecurityFinding>
        {
            new SecurityFinding { Severity = SecuritySeverity.Critical, Title = "Critical Issue" }
        };
        var policy = new SecurityPolicy { CriticalSeverityAction = PolicyAction.Warn };

        // Act
        var result = _engine.Evaluate(findings, policy);

        // Assert
        result.IsSuccess.Should().BeTrue("Warn action does not fail the policy");
        result.WarnCount.Should().Be(1);
        result.Violations.Should().ContainSingle(v => v.Action == PolicyAction.Warn);
    }

    [Fact]
    public void Evaluate_ShouldIgnore_WhenRuleIsIgnored()
    {
        // Arrange
        var findings = new List<SecurityFinding>
        {
            new SecurityFinding { RuleId = "PS0001", Severity = SecuritySeverity.Critical }
        };
        var policy = new SecurityPolicy();
        policy.IgnoredRules.Add("PS0001");

        // Act
        var result = _engine.Evaluate(findings, policy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Violations.Should().ContainSingle(v => v.Action == PolicyAction.Ignore);
    }
}

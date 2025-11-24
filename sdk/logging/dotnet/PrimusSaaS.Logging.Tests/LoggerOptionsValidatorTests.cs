using PrimusSaaS.Logging.Core;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class LoggerOptionsValidatorTests
{
    [Fact]
    public void Validate_ShouldThrow_WhenApplicationIdMissing()
    {
        var options = new LoggerOptions
        {
            ApplicationId = "",
            Environment = "dev"
        };

        Assert.Throws<ArgumentException>(() => LoggerOptionsValidator.Validate(options));
    }

    [Fact]
    public void Validate_ShouldThrow_OnUnsupportedTarget()
    {
        var options = new LoggerOptions
        {
            ApplicationId = "APP",
            Environment = "dev",
            Targets = new List<TargetConfig>
            {
                new() { Type = "unknown" }
            }
        };

        Assert.Throws<ArgumentException>(() => LoggerOptionsValidator.Validate(options));
    }

    [Fact]
    public void Validate_ShouldPass_ForValidConfiguration()
    {
        var options = new LoggerOptions
        {
            ApplicationId = "APP",
            Environment = "dev",
            Targets = new List<TargetConfig>
            {
                new() { Type = "console" }
            }
        };

        LoggerOptionsValidator.Validate(options);
    }
}

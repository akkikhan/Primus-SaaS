using FluentAssertions;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class LoggingOptionsTests
{
    [Fact]
    public void LoggingOptions_Defaults()
    {
        var options = new PrimusIdentityLoggingOptions();
        options.MinimumLevel.Should().Be(LogLevel.Information);
        options.RedactSensitiveData.Should().BeTrue();
        options.LogValidationSteps.Should().BeTrue();
        options.LogClaimMapping.Should().BeFalse();
    }
}

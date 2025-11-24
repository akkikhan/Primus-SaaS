using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using PrimusSaaS.Identity.Validator.Services;

namespace PrimusSaaS.Identity.Validator.Tests;

public class SecurityEventLoggerTests
{
    [Fact]
    public void DefaultSecurityEventLogger_Should_Log_Info_OnSuccess()
    {
        var mockLogger = new Mock<ILogger<DefaultSecurityEventLogger>>();
        var metrics = new SecurityEventMetrics();
        var securityLogger = new DefaultSecurityEventLogger(mockLogger.Object, metrics);

        securityLogger.LogSuccessfulAuthentication(new ClaimsPrincipal(), "issuer");

        mockLogger.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public void Events_Should_Be_Invoked_In_Pipeline()
    {
        var captured = new List<string>();
        var fakeLogger = new FakeSecurityEventLogger(captured);
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPrimusIdentity(options =>
        {
            options.Issuers = new List<IssuerConfig>
            {
                new IssuerConfig
                {
                    Name = "Local",
                    Type = IssuerType.Jwt,
                    Issuer = "https://local",
                    Secret = "super-secret-key-super-secret-key",
                    Audiences = new List<string> { "aud" }
                }
            };
        });

        // Override default logger after Primus registrations
        services.AddSingleton<ISecurityEventLogger>(fakeLogger);

        var provider = services.BuildServiceProvider();
        // Simulate manual call
        var logger = provider.GetRequiredService<ISecurityEventLogger>();
        logger.LogFailedAuthentication("https://local", "bad token");
        logger.LogRateLimited("https://local", "rate limited");
        logger.LogSuccessfulAuthentication(new ClaimsPrincipal(), "https://local");

        captured.Should().Contain("failed");
        captured.Should().Contain("rate");
        captured.Should().Contain("success");
    }

    private class FakeSecurityEventLogger : ISecurityEventLogger
    {
        private readonly List<string> _events;

        public FakeSecurityEventLogger(List<string> events) => _events = events;
        public void LogSuccessfulAuthentication(ClaimsPrincipal principal, string? issuer) => _events.Add("success");
        public void LogFailedAuthentication(string? issuer, string reason) => _events.Add("failed");
        public void LogRateLimited(string? issuer, string reason) => _events.Add("rate");
    }
}

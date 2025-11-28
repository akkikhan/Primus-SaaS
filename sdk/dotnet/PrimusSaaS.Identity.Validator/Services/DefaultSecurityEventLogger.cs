using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator.Services;
/// <summary>
/// Default security event logger that forwards to ILogger.
/// </summary>
public class DefaultSecurityEventLogger : ISecurityEventLogger
{
    private readonly ILogger<DefaultSecurityEventLogger> _logger;
    private readonly SecurityEventMetrics _metrics;
    private readonly PrimusIdentityLoggingOptions _loggingOptions;
    public DefaultSecurityEventLogger(ILogger<DefaultSecurityEventLogger> logger, SecurityEventMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
        _loggingOptions = new PrimusIdentityLoggingOptions();
    }

    public DefaultSecurityEventLogger(ILogger<DefaultSecurityEventLogger> logger, SecurityEventMetrics metrics, PrimusIdentityLoggingOptions loggingOptions)
    {
        _logger = logger;
        _metrics = metrics;
        _loggingOptions = loggingOptions ?? new PrimusIdentityLoggingOptions();
    }

    public void LogSuccessfulAuthentication(ClaimsPrincipal principal, string? issuer)
    {
        _metrics.IncrementSuccess();
        _logger.Log(_loggingOptions.MinimumLevel, "Primus Identity: Authentication succeeded for issuer {Issuer}", issuer ?? "unknown");
    }

    public void LogFailedAuthentication(string? issuer, string reason)
    {
        _metrics.IncrementFailure();
        _logger.Log(_loggingOptions.MinimumLevel, "Primus Identity: Authentication failed for issuer {Issuer}. Reason: {Reason}", issuer ?? "unknown", reason);
    }

    public void LogRateLimited(string? issuer, string reason)
    {
        _metrics.IncrementRateLimited();
        _logger.Log(_loggingOptions.MinimumLevel, "Primus Identity: Authentication attempts rate-limited for issuer {Issuer}. Reason: {Reason}", issuer ?? "unknown", reason);
    }
}

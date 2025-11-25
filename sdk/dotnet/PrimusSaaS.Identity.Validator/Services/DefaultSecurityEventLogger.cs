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
    public DefaultSecurityEventLogger(ILogger<DefaultSecurityEventLogger> logger, SecurityEventMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public void LogSuccessfulAuthentication(ClaimsPrincipal principal, string? issuer)
    {
        _metrics.IncrementSuccess();
        _logger.LogInformation("Primus Identity: Authentication succeeded for issuer {Issuer}", issuer ?? "unknown");
    }

    public void LogFailedAuthentication(string? issuer, string reason)
    {
        _metrics.IncrementFailure();
        _logger.LogWarning("Primus Identity: Authentication failed for issuer {Issuer}. Reason: {Reason}", issuer ?? "unknown", reason);
    }

    public void LogRateLimited(string? issuer, string reason)
    {
        _metrics.IncrementRateLimited();
        _logger.LogWarning("Primus Identity: Authentication attempts rate-limited for issuer {Issuer}. Reason: {Reason}", issuer ?? "unknown", reason);
    }
}

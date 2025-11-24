using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Emits security-relevant events for observability/auditing.
/// </summary>
public interface ISecurityEventLogger
{
    void LogSuccessfulAuthentication(ClaimsPrincipal principal, string? issuer);
    void LogFailedAuthentication(string? issuer, string reason);
    void LogRateLimited(string? issuer, string reason);
}

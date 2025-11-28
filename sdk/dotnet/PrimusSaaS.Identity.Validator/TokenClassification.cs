using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Helper methods to classify tokens (e.g., M2M) and validate grant-specific rules.
/// </summary>
public static class TokenClassification
{
    /// <summary>
    /// Detects machine-to-machine tokens using common Auth0 heuristics (sub ending with @clients or gty=client-credentials).
    /// </summary>
    public static bool IsMachineToMachine(ClaimsPrincipal? principal)
    {
        if (principal == null) return false;
        var sub = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var grantType = principal.FindFirst("gty")?.Value;

        if (!string.IsNullOrWhiteSpace(grantType) &&
            string.Equals(grantType, "client-credentials", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return sub != null && sub.EndsWith("@clients", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that M2M tokens are allowed and grant type is permitted.
    /// </summary>
    public static bool ValidateMachineToMachineAllowed(ClaimsPrincipal? principal, IssuerConfig issuerConfig, out string? error)
    {
        error = null;
        var isM2M = IsMachineToMachine(principal);
        if (!isM2M)
        {
            return true;
        }

        if (!issuerConfig.AllowMachineToMachine)
        {
            error = "Machine-to-machine tokens are not allowed for this issuer.";
            return false;
        }

        if (issuerConfig.AllowedGrantTypes != null && issuerConfig.AllowedGrantTypes.Any())
        {
            var grantType = principal?.FindFirst("gty")?.Value;
            if (!string.IsNullOrWhiteSpace(grantType) &&
                !issuerConfig.AllowedGrantTypes.Contains(grantType, StringComparer.OrdinalIgnoreCase))
            {
                error = $"Grant type '{grantType}' is not allowed for this issuer.";
                return false;
            }
        }

        if (issuerConfig.AllowedMachineToMachineScopes != null && issuerConfig.AllowedMachineToMachineScopes.Any())
        {
            var tokenScopes = ParseScopes(principal);
            if (tokenScopes.Count == 0)
            {
                error = "Machine-to-machine token missing scopes.";
                return false;
            }

            var allowed = issuerConfig.AllowedMachineToMachineScopes;
            if (tokenScopes.Any(scope => !allowed.Contains(scope, StringComparer.Ordinal)))
            {
                error = "Machine-to-machine token contains disallowed scopes.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true when email_verified claim is present and true; missing claim is treated as unknown (false).
    /// </summary>
    public static bool IsEmailVerified(ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirst("email_verified")?.Value;
        return value != null && bool.TryParse(value, out var verified) && verified;
    }

    private static List<string> ParseScopes(ClaimsPrincipal? principal)
    {
        var scopeClaim = principal?.FindFirst("scope")?.Value;
        if (string.IsNullOrWhiteSpace(scopeClaim))
        {
            return new List<string>();
        }

        return scopeClaim
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}

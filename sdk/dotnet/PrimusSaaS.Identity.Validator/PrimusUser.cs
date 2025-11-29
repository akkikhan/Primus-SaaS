using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Represents a user authenticated via a configured issuer.
/// </summary>
public class PrimusUser
{
    /// <summary>
    /// The unique subject identifier from the token.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The user's full name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Additional claims from the JWT token.
    /// </summary>
    public Dictionary<string, string> AdditionalClaims { get; set; } = new();

    /// <summary>
    /// The issuer string from the token (iss claim).
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Matched issuer/provider name injected by Primus Identity (when available).
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Matched issuer/provider type injected by Primus Identity (when available).
    /// </summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>
    /// Identity provider extracted from the subject prefix (e.g., auth0, google-oauth2).
    /// </summary>
    public string IdentityProvider { get; set; } = string.Empty;

    /// <summary>
    /// True when subject indicates a social login (provider|id) and provider is not "auth0".
    /// </summary>
    public bool IsSocialLogin { get; set; }

    /// <summary>
    /// True when token appears to be machine-to-machine (client credentials flow).
    /// </summary>
    public bool IsMachineToMachine { get; set; }

    /// <summary>
    /// Optional client id for machine-to-machine tokens (from sub or azp).
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Creates a PrimusUser from ClaimsPrincipal.
    /// </summary>
    public static PrimusUser FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        if (principal == null)
            throw new ArgumentNullException(nameof(principal));

        var user = new PrimusUser
        {
            UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            Name = principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
            Issuer = principal.FindFirst("iss")?.Value ?? string.Empty,
            ProviderName = principal.FindFirst("primus:issuer_name")?.Value ?? string.Empty,
            ProviderType = principal.FindFirst("primus:issuer_type")?.Value ?? string.Empty
        };

        // Identity provider parsing from Auth0-style subject: provider|id
        var subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(subject) && subject.Contains('|'))
        {
            var parts = subject.Split('|', 2);
            user.IdentityProvider = parts[0];
            user.IsSocialLogin = !string.Equals(parts[0], "auth0", StringComparison.OrdinalIgnoreCase);
        }

        // Machine-to-machine detection heuristics (Auth0): sub ends with @clients or gty/client-credentials
        var subRaw = principal.FindFirst("sub")?.Value ?? subject;
        var grantType = principal.FindFirst("gty")?.Value;
        var isM2M = (subRaw?.EndsWith("@clients", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    string.Equals(grantType, "client-credentials", StringComparison.OrdinalIgnoreCase);
        user.IsMachineToMachine = isM2M;
        if (isM2M)
        {
            // For Auth0 M2M tokens, sub is typically client-id@clients; azp also carries client id
            user.ClientId = subRaw?.Replace("@clients", string.Empty, StringComparison.OrdinalIgnoreCase)
                           ?? principal.FindFirst("azp")?.Value;
        }

        // Capture additional claims
        foreach (var claim in principal.Claims)
        {
            if (claim.Type != ClaimTypes.NameIdentifier &&
                claim.Type != ClaimTypes.Email &&
                claim.Type != ClaimTypes.Name &&
                claim.Type != ClaimTypes.Role)
            {
                user.AdditionalClaims[claim.Type] = claim.Value;
            }
        }

        return user;
    }
}

/// <summary>
/// Extension methods for accessing Primus user information from HttpContext.
/// </summary>
public static class PrimusUserExtensions
{
    /// <summary>
    /// Gets the current Primus user from the HTTP context.
    /// </summary>
    public static PrimusUser? GetPrimusUser(this Microsoft.AspNetCore.Http.HttpContext? context)
    {
        if (context == null)
            return null;

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return PrimusUser.FromClaimsPrincipal(context.User);
        }

        return null;
    }
}

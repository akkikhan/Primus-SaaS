using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Represents a user authenticated via Primus SaaS Portal.
/// </summary>
public class PrimusUser
{
    /// <summary>
    /// The unique user ID from Primus SaaS Portal.
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
            Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        };

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

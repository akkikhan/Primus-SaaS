using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Extension methods for retrieving tenant context.
/// </summary>
public static class TenantContextExtensions
{
    /// <summary>
    /// Gets the current tenant context from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The tenant context, or null if not resolved.</returns>
    public static TenantContext? GetTenantContext(this HttpContext? context)
    {
        if (context == null)
            return null;

        if (context.Items.TryGetValue("TenantContext", out var value) && value is TenantContext tenantContext)
        {
            return tenantContext;
        }

        return null;
    }

    /// <summary>
    /// Gets the current tenant ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The tenant ID, or null if not resolved.</returns>
    public static string? GetTenantId(this HttpContext? context)
    {
        return context?.GetTenantContext()?.TenantId;
    }
}

/// <summary>
/// Extension methods for ClaimsPrincipal to easily retrieve claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the value of a specific claim type.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="claimType">The claim type to retrieve.</param>
    /// <returns>The claim value, or null if not found.</returns>
    public static string? Get(this ClaimsPrincipal principal, string claimType)
    {
        return principal?.FindFirst(claimType)?.Value;
    }

    /// <summary>
    /// Gets a list of values for a specific claim type.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="claimType">The claim type to retrieve.</param>
    /// <returns>A list of claim values.</returns>
    public static List<string> GetList(this ClaimsPrincipal principal, string claimType)
    {
        return principal?.FindAll(claimType).Select(c => c.Value).ToList() ?? new List<string>();
    }

    /// <summary>
    /// Gets the value of a specific claim type converted to type T.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="principal">The claims principal.</param>
    /// <param name="claimType">The claim type to retrieve.</param>
    /// <returns>The converted value, or default(T) if not found or conversion fails.</returns>
    public static T? Get<T>(this ClaimsPrincipal principal, string claimType)
    {
        var value = principal?.Get(claimType);
        if (string.IsNullOrEmpty(value))
            return default;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}

using Microsoft.AspNetCore.Http;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Options for configuring multiple Auth0 tenants and resolving which tenant to use per request.
/// </summary>
public class Auth0MultiTenantOptions
{
    /// <summary>
    /// Collection of tenants keyed by tenant identifier.
    /// </summary>
    public Dictionary<string, Auth0Options> Tenants { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tenant resolution function based on HttpContext (e.g., host header, path, or header).
    /// </summary>
    public Func<HttpContext, string?>? ResolveTenant { get; set; }

    /// <summary>
    /// Optional fallback to resolve from token issuer when request-based resolution is absent.
    /// </summary>
    public bool ResolveFromIssuerWhenUnknown { get; set; } = true;
}

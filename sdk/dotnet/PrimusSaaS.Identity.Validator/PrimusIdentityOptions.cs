using PrimusSaaS.Identity.Validator.Services;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Type of identity issuer.
/// </summary>
public enum IssuerType
{
    /// <summary>
    /// OpenID Connect issuer (e.g., Azure AD).
    /// </summary>
    Oidc = 0,

    /// <summary>
    /// Auth0 issuer (alias for OIDC to keep DX discoverable).
    /// </summary>
    Auth0 = Oidc,

    /// <summary>
    /// Google issuer (alias for OIDC to keep DX discoverable).
    /// </summary>
    Google = Oidc,

    /// <summary>
    /// Cognito issuer (alias for OIDC to keep DX discoverable).
    /// </summary>
    Cognito = Oidc,

    /// <summary>
    /// Azure AD issuer (alias for OIDC to improve discoverability).
    /// </summary>
    AzureAD = Oidc,

    /// <summary>
    /// JWT issuer using shared secret (e.g., Local Auth).
    /// </summary>
    Jwt = 1
}

/// <summary>
/// Configuration for a single identity issuer.
/// </summary>
public class IssuerConfig
{
    /// <summary>
    /// Friendly name for this issuer (e.g., "AzureAD", "LocalAuth").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of issuer (Oidc/AzureAD or Jwt).
    /// </summary>
    public IssuerType Type { get; set; }

    /// <summary>
    /// The 'iss' claim value to match in the token.
    /// Used to route the token to the correct validator.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// For OIDC: The authority URL (e.g., https://login.microsoftonline.com/...).
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// For JWT: The JWKS endpoint URL (optional if Secret is provided).
    /// </summary>
    public string? JwksUrl { get; set; }

    /// <summary>
    /// For JWT (Local Dev): Shared secret key.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Valid audiences for this issuer.
    /// </summary>
    public List<string> Audiences { get; set; } = new();

    /// <summary>
    /// Optional claim mappings to normalize provider-specific claims into common claim types.
    /// Key = source claim type, Value = target claim type.
    /// </summary>
    public Dictionary<string, string> ClaimMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional custom role claim name (mapped into ClaimTypes.Role when provided).
    /// </summary>
    public string? RoleClaimName { get; set; }

    /// <summary>
    /// Optional custom permission claim name (mirrored into PrimusClaimTypes.Permission).
    /// </summary>
    public string PermissionClaimName { get; set; } = PrimusClaimTypes.Permission;

    /// <summary>
    /// Optional organization claim name (mirrored into PrimusClaimTypes.Organization when provided).
    /// </summary>
    public string OrganizationClaimName { get; set; } = PrimusClaimTypes.Organization;

    /// <summary>
    /// Whether to enforce presence (and optionally a specific value) for the organization claim.
    /// </summary>
    public bool ValidateOrganization { get; set; }

    /// <summary>
    /// Optional required organization value when ValidateOrganization is enabled.
    /// </summary>
    public string? RequiredOrganization { get; set; }

    /// <summary>
    /// Whether machine-to-machine tokens (client credentials) are allowed for this issuer.
    /// </summary>
    public bool AllowMachineToMachine { get; set; }

    /// <summary>
    /// Allowed grant types for machine-to-machine tokens (e.g., client-credentials).
    /// </summary>
    public List<string> AllowedGrantTypes { get; set; } = new();

    /// <summary>
    /// Allowed scopes for machine-to-machine tokens (optional). If set, incoming scopes must be a superset.
    /// </summary>
    public List<string> AllowedMachineToMachineScopes { get; set; } = new();

    /// <summary>
    /// Require email_verified=true for user tokens (non-M2M) when true.
    /// </summary>
    public bool RequireEmailVerification { get; set; }
}

/// <summary>
/// Configuration options for Primus SaaS Identity validation.
/// </summary>
public class PrimusIdentityOptions
{
    /// <summary>
    /// List of trusted identity providers.
    /// </summary>
    public List<IssuerConfig> Issuers { get; set; } = new();

    /// <summary>
    /// Multi-tenant Auth0 configuration (optional).
    /// </summary>
    public Auth0MultiTenantOptions? Auth0MultiTenant { get; set; }

    /// <summary>
    /// Logging options for authentication events and validation flow.
    /// </summary>
    public PrimusIdentityLoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Whether to validate the token lifetime. Default is true.
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to require HTTPS for metadata and token endpoints. Default is true.
    /// Set to false only for development environments.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Allow HTTP issuers/authorities when targeting localhost/loopback hosts (for local development).
    /// </summary>
    public bool AllowHttpOnLocalhost { get; set; } = true;

    /// <summary>
    /// Clock skew to allow for time differences between servers. Default is 5 minutes.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Time-to-live for JWKS cache (OIDC mode only). Default is 24 hours.
    /// </summary>
    public TimeSpan JwksCacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// API key authentication options (optional; disabled by default).
    /// </summary>
    public ApiKeyOptions ApiKey { get; set; } = new();

    /// <summary>
    /// Per-tenant/API key request rate limiting options.
    /// </summary>
    public TenantRateLimitOptions TenantRateLimiting { get; set; } = new();

    /// <summary>
    /// Validates that all required options are configured.
    /// </summary>
    public void Validate()
    {
        var errors = new List<string>();

        if (Issuers == null || !Issuers.Any())
        {
            errors.Add("At least one issuer configuration is required.");
            ThrowIfErrors();
            return;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var claimIssuers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var issuer in Issuers)
        {
            ValidateIssuer(issuer, names, claimIssuers, errors, AllowHttpOnLocalhost);
        }

        try
        {
            ApiKey?.Validate();
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        ThrowIfErrors();

        void ThrowIfErrors()
        {
            if (errors.Count == 0) return;
            throw new ArgumentException($"Primus Identity configuration invalid: {string.Join("; ", errors)}");
        }
    }

    private static void ValidateIssuer(
        IssuerConfig issuer,
        HashSet<string> names,
        HashSet<string> claimIssuers,
        List<string> errors,
        bool allowHttpOnLocalhost)
    {
        if (string.IsNullOrWhiteSpace(issuer.Name))
        {
            errors.Add("Issuer name is required.");
        }
        else if (!names.Add(issuer.Name))
        {
            errors.Add($"Duplicate issuer name '{issuer.Name}'. Names must be unique.");
        }

        if (string.IsNullOrWhiteSpace(issuer.Issuer))
        {
            errors.Add($"Issuer claim value is required for '{issuer.Name}'.");
        }
        else if (!claimIssuers.Add(issuer.Issuer))
        {
            errors.Add($"Duplicate issuer claim value '{issuer.Issuer}'. Configure unique 'Issuer' per identity provider.");
        }

        if (!IsAllowedIssuerUri(issuer.Issuer, allowHttpOnLocalhost))
        {
            var localhostNote = allowHttpOnLocalhost ? " HTTP is allowed for localhost during development." : string.Empty;
            errors.Add($"Issuer must be an absolute HTTPS URI for '{issuer.Name}'.{localhostNote} Example: https://your-tenant.auth0.com/.");
        }

        if (issuer.Audiences == null || !issuer.Audiences.Any() || issuer.Audiences.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"At least one non-empty audience is required for '{issuer.Name}'.");
        }

        if (issuer.Type.IsOidcBased())
        {
            if (string.IsNullOrWhiteSpace(issuer.Authority))
            {
                errors.Add($"Authority URL is required for OIDC/AzureAD issuer '{issuer.Name}'.");
            }
            else if (!Uri.TryCreate(issuer.Authority, UriKind.Absolute, out var authorityUri))
            {
                errors.Add($"Authority must be an absolute URI for '{issuer.Name}'. Example: https://login.microsoftonline.com/<tenant-id>.");
            }
            else if (!IsAllowedAuthorityUri(authorityUri, allowHttpOnLocalhost))
            {
                var localhostNote = allowHttpOnLocalhost ? " HTTP is allowed for localhost during development." : string.Empty;
                errors.Add($"Authority must use HTTPS for '{issuer.Name}'.{localhostNote}");
            }
        }
        else if (issuer.Type == IssuerType.Jwt)
        {
            var hasSecret = !string.IsNullOrWhiteSpace(issuer.Secret);
            var hasJwks = !string.IsNullOrWhiteSpace(issuer.JwksUrl);
            if (!hasSecret && !hasJwks)
            {
                errors.Add($"Secret or JWKS URL is required for JWT issuer '{issuer.Name}'.");
            }

            if (hasJwks && !Uri.TryCreate(issuer.JwksUrl, UriKind.Absolute, out _))
            {
                errors.Add($"JWKS URL must be an absolute URI for '{issuer.Name}'.");
            }
        }

        if (issuer.ClaimMappings.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value)))
        {
            errors.Add($"ClaimMappings for '{issuer.Name}' cannot contain empty keys or values.");
        }

        if (issuer.ValidateOrganization)
        {
            if (string.IsNullOrWhiteSpace(issuer.OrganizationClaimName))
            {
                errors.Add($"OrganizationClaimName is required when ValidateOrganization=true for '{issuer.Name}'.");
            }
        }

        if (issuer.AllowMachineToMachine && issuer.AllowedGrantTypes.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"AllowedGrantTypes cannot contain empty entries for '{issuer.Name}'.");
        }

        if (issuer.AllowMachineToMachine && issuer.AllowedMachineToMachineScopes.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"AllowedMachineToMachineScopes cannot contain empty entries for '{issuer.Name}'.");
        }
    }

    private static bool IsAllowedIssuerUri(string value, bool allowHttpOnLocalhost)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return true;
        return allowHttpOnLocalhost && uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && uri.IsLoopback;
    }

    private static bool IsAllowedAuthorityUri(Uri uri, bool allowHttpOnLocalhost)
    {
        if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return true;
        return allowHttpOnLocalhost && uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && uri.IsLoopback;
    }

    /// <summary>
    /// Optional function to resolve tenant context from token claims.
    /// </summary>
    public Func<TokenClaims, TenantContext?>? TenantResolver { get; set; }

    /// <summary>
    /// Rate limiting options for failed token validations.
    /// </summary>
    public FailedValidationRateLimiterOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// Token refresh options (optional; disabled by default).
    /// </summary>
    public TokenRefreshOptions TokenRefresh { get; set; } = new();
}

/// <summary>
/// Represents the resolved tenant context.
/// </summary>
public class TenantContext
{
    /// <summary>
    /// The unique tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The user's roles within this tenant.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Additional tenant-specific metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Wrapper around token claims for easier access.
/// Implements IEnumerable to support LINQ operations.
/// </summary>
/// <example>
/// <code>
/// options.TenantResolver = claims =>
/// {
///     // Access claims using Get method
///     var tenantId = claims.Get("tid");
///     
///     // Or use LINQ (now supported!)
///     var roles = claims.Where(c => c.Key.StartsWith("role_"))
///                       .Select(c => c.Value.ToString())
///                       .ToList();
///     
///     return new TenantContext 
///     { 
///         TenantId = tenantId ?? "default",
///         Roles = roles
///     };
/// };
/// </code>
/// </example>
public class TokenClaims : IEnumerable<KeyValuePair<string, object>>
{
    private readonly Dictionary<string, object> _claims;

    public TokenClaims(Dictionary<string, object> claims)
    {
        _claims = claims ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets a claim value as a string.
    /// </summary>
    /// <param name="claimType">The claim type to retrieve.</param>
    /// <returns>The claim value as a string, or null if not found.</returns>
    public string? Get(string claimType)
    {
        if (!_claims.TryGetValue(claimType, out var val) || val == null)
            return null;

        if (val is IEnumerable<string> stringEnumerable)
            return stringEnumerable.FirstOrDefault();

        if (val is IEnumerable<object> objectEnumerable)
            return objectEnumerable.FirstOrDefault()?.ToString();

        return val.ToString();
    }
    
    /// <summary>
    /// Gets a claim value as a specific type.
    /// </summary>
    /// <typeparam name="T">The type to convert the claim value to.</typeparam>
    /// <param name="claimType">The claim type to retrieve.</param>
    /// <returns>The claim value as type T, or default(T) if not found or cannot convert.</returns>
    public T? Get<T>(string claimType)
    {
        if (_claims.TryGetValue(claimType, out var val) && val is T typedVal)
            return typedVal;
        return default;
    }

    /// <summary>
    /// Gets all claims as a dictionary.
    /// </summary>
    public Dictionary<string, object> All => _claims;

    /// <summary>
    /// Gets the first claim value that matches the predicate, or null.
    /// </summary>
    /// <param name="predicate">The predicate to match claims.</param>
    /// <returns>The first matching claim value as a string, or null.</returns>
    public string? FirstOrDefault(Func<KeyValuePair<string, object>, bool> predicate)
    {
        var match = _claims.FirstOrDefault(predicate);
        return match.Value?.ToString();
    }

    /// <summary>
    /// Filters claims based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter claims.</param>
    /// <returns>Enumerable of matching claims.</returns>
    public IEnumerable<KeyValuePair<string, object>> Where(Func<KeyValuePair<string, object>, bool> predicate)
    {
        return _claims.Where(predicate);
    }

    /// <summary>
    /// Checks if a claim exists.
    /// </summary>
    /// <param name="claimType">The claim type to check.</param>
    /// <returns>True if the claim exists, false otherwise.</returns>
    public bool Contains(string claimType) => _claims.ContainsKey(claimType);

    /// <summary>
    /// Gets the number of claims.
    /// </summary>
    public int Count => _claims.Count;

    /// <summary>
    /// Implements IEnumerable to support LINQ operations.
    /// </summary>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _claims.GetEnumerator();

    /// <summary>
    /// Implements IEnumerable to support LINQ operations.
    /// </summary>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _claims.GetEnumerator();
}

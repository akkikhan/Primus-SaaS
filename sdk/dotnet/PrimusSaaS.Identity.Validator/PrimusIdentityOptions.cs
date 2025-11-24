namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Type of identity issuer.
/// </summary>
public enum IssuerType
{
    /// <summary>
    /// OpenID Connect issuer (e.g., Azure AD).
    /// </summary>
    Oidc,

    /// <summary>
    /// JWT issuer using shared secret (e.g., Local Auth).
    /// </summary>
    Jwt
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
    /// Type of issuer (Oidc or Jwt).
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
    /// Whether to validate the token lifetime. Default is true.
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to require HTTPS for metadata and token endpoints. Default is true.
    /// Set to false only for development environments.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Clock skew to allow for time differences between servers. Default is 5 minutes.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Time-to-live for JWKS cache (OIDC mode only). Default is 24 hours.
    /// </summary>
    public TimeSpan JwksCacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Validates that all required options are configured.
    /// </summary>
    public void Validate()
    {
        if (Issuers == null || !Issuers.Any())
            throw new ArgumentException("At least one issuer configuration is required.", nameof(Issuers));

        foreach (var issuer in Issuers)
        {
            if (string.IsNullOrWhiteSpace(issuer.Name))
                throw new ArgumentException("Issuer name is required.");

            if (string.IsNullOrWhiteSpace(issuer.Issuer))
                throw new ArgumentException($"Issuer claim value is required for {issuer.Name}.");

            if (issuer.Audiences == null || !issuer.Audiences.Any())
                throw new ArgumentException($"At least one audience is required for {issuer.Name}.");

            if (issuer.Type == IssuerType.Oidc && string.IsNullOrWhiteSpace(issuer.Authority))
                throw new ArgumentException($"Authority URL is required for OIDC issuer {issuer.Name}.");

            if (issuer.Type == IssuerType.Jwt && string.IsNullOrWhiteSpace(issuer.Secret) && string.IsNullOrWhiteSpace(issuer.JwksUrl))
                throw new ArgumentException($"Secret or JWKS URL is required for JWT issuer {issuer.Name}.");
        }
    }

    /// <summary>
    /// Optional function to resolve tenant context from token claims.
    /// </summary>
    public Func<TokenClaims, TenantContext>? TenantResolver { get; set; }
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
    public string? Get(string claimType) => _claims.TryGetValue(claimType, out var val) ? val?.ToString() : null;
    
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

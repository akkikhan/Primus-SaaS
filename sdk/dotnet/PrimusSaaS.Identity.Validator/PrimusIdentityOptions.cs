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
}

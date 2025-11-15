namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Authentication validation mode.
/// </summary>
public enum ValidationMode
{
    /// <summary>
    /// Local JWT validation using symmetric key (HMAC).
    /// Portal generates and signs tokens directly.
    /// </summary>
    Local,

    /// <summary>
    /// Azure AD validation using asymmetric keys (RS256).
    /// Tokens are issued by Azure AD and validated using JWKS.
    /// </summary>
    AzureAd,

    /// <summary>
    /// Hybrid mode: supports both Local and Azure AD validation.
    /// Automatically detects token type based on issuer.
    /// </summary>
    Hybrid
}

/// <summary>
/// Configuration options for Primus SaaS Identity validation.
/// </summary>
public class PrimusIdentityOptions
{
    /// <summary>
    /// The validation mode to use. Defaults to Local for backward compatibility.
    /// </summary>
    public ValidationMode Mode { get; set; } = ValidationMode.Local;

    /// <summary>
    /// The base URL of the Primus SaaS Portal (e.g., https://portal.primus-saas.com).
    /// </summary>
    public string PortalUrl { get; set; } = string.Empty;

    /// <summary>
    /// The Client ID for your application registered in Primus SaaS Portal.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The Client Secret for your application registered in Primus SaaS Portal.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The JWT secret key used to validate tokens (Local mode only). Retrieved from the portal.
    /// </summary>
    public string? JwtSecret { get; set; }

    /// <summary>
    /// The Azure AD Tenant ID (AzureAd or Hybrid mode only).
    /// Format: GUID (e.g., 12345678-1234-1234-1234-123456789012).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// The issuer value expected in JWT tokens. Defaults to portal URL (Local) or Azure AD issuer (AzureAd).
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// The audience value expected in JWT tokens. Defaults to ClientId if not specified.
    /// </summary>
    public string? Audience { get; set; }

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
    /// Time-to-live for JWKS cache (AzureAd mode only). Default is 24 hours.
    /// </summary>
    public TimeSpan JwksCacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Validates that all required options are configured.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PortalUrl))
            throw new ArgumentException("PortalUrl is required.", nameof(PortalUrl));

        if (string.IsNullOrWhiteSpace(ClientId))
            throw new ArgumentException("ClientId is required.", nameof(ClientId));

        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new ArgumentException("ClientSecret is required.", nameof(ClientSecret));

        if (!Uri.IsWellFormedUriString(PortalUrl, UriKind.Absolute))
            throw new ArgumentException("PortalUrl must be a valid absolute URI.", nameof(PortalUrl));

        // Mode-specific validation
        switch (Mode)
        {
            case ValidationMode.Local:
                if (string.IsNullOrWhiteSpace(JwtSecret))
                    throw new ArgumentException("JwtSecret is required for Local mode.", nameof(JwtSecret));
                break;

            case ValidationMode.AzureAd:
                if (string.IsNullOrWhiteSpace(TenantId))
                    throw new ArgumentException("TenantId is required for AzureAd mode.", nameof(TenantId));
                if (!Guid.TryParse(TenantId, out _))
                    throw new ArgumentException("TenantId must be a valid GUID.", nameof(TenantId));
                break;

            case ValidationMode.Hybrid:
                if (string.IsNullOrWhiteSpace(JwtSecret))
                    throw new ArgumentException("JwtSecret is required for Hybrid mode.", nameof(JwtSecret));
                if (string.IsNullOrWhiteSpace(TenantId))
                    throw new ArgumentException("TenantId is required for Hybrid mode.", nameof(TenantId));
                if (!Guid.TryParse(TenantId, out _))
                    throw new ArgumentException("TenantId must be a valid GUID.", nameof(TenantId));
                break;
        }

        // Set defaults if not provided
        Issuer ??= PortalUrl;
        Audience ??= ClientId;
    }
}

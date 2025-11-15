namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Configuration options for Primus SaaS Identity validation.
/// </summary>
public class PrimusIdentityOptions
{
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
    /// The JWT secret key used to validate tokens. Retrieved from the portal.
    /// </summary>
    public string JwtSecret { get; set; } = string.Empty;

    /// <summary>
    /// The issuer value expected in JWT tokens. Defaults to portal URL if not specified.
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

        if (string.IsNullOrWhiteSpace(JwtSecret))
            throw new ArgumentException("JwtSecret is required.", nameof(JwtSecret));

        if (!Uri.IsWellFormedUriString(PortalUrl, UriKind.Absolute))
            throw new ArgumentException("PortalUrl must be a valid absolute URI.", nameof(PortalUrl));

        // Set defaults if not provided
        Issuer ??= PortalUrl;
        Audience ??= ClientId;
    }
}

using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Configuration options for Google OIDC validation.
/// </summary>
public class GoogleOptions
{
    /// <summary>
    /// Allowed audiences (your Google OAuth client IDs).
    /// </summary>
    public List<string> Audiences { get; set; } = new();

    /// <summary>
    /// Optional friendly name (defaults to "Google").
    /// </summary>
    public string Name { get; set; } = "Google";

    /// <summary>
    /// Optional claim mappings to normalize Google claims into standard claim types.
    /// </summary>
    public Dictionary<string, string> ClaimMappings { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        { "sub", ClaimTypes.NameIdentifier },
        { "email", ClaimTypes.Email },
        { "name", ClaimTypes.Name }
    };

    internal IssuerConfig ToIssuerConfig()
    {
        var issuer = "https://accounts.google.com";
        var audiences = Audiences.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.Ordinal).ToList();
        if (!audiences.Any())
        {
            throw new ArgumentException("At least one audience (Google client ID) is required.", nameof(Audiences));
        }

        return new IssuerConfig
        {
            Name = string.IsNullOrWhiteSpace(Name) ? "Google" : Name,
            Type = IssuerType.Google,
            Issuer = issuer + "/",
            Authority = issuer,
            Audiences = audiences,
            ClaimMappings = new Dictionary<string, string>(ClaimMappings, StringComparer.OrdinalIgnoreCase)
        };
    }
}

public static class GooglePrimusIdentityExtensions
{
    /// <summary>
    /// Adds Google as an issuer using the standard Google OIDC configuration.
    /// </summary>
    public static PrimusIdentityOptions UseGoogle(this PrimusIdentityOptions options, string audience, Action<GoogleOptions>? configure = null)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        var google = new GoogleOptions();
        if (!string.IsNullOrWhiteSpace(audience))
        {
            google.Audiences.Add(audience);
        }
        configure?.Invoke(google);
        options.Issuers.Add(google.ToIssuerConfig());
        return options;
    }

    /// <summary>
    /// Adds Google using the provider-style API.
    /// </summary>
    public static PrimusIdentityOptions AddProvider(this PrimusIdentityOptions options, IdentityProvider provider, Action<GoogleOptions> configure)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (provider != IdentityProvider.Google) throw new ArgumentException("This overload is only for Google providers.", nameof(provider));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var google = new GoogleOptions();
        configure(google);
        options.Issuers.Add(google.ToIssuerConfig());
        return options;
    }
}

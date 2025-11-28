using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Configuration options for Auth0 provider integration.
/// </summary>
public class Auth0Options
{
    private string? _name;

    /// <summary>
    /// Auth0 tenant domain (e.g., your-tenant.auth0.com).
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Allowed audiences (API identifiers). At least one is required.
    /// </summary>
    public List<string> Audiences { get; set; } = new();

    /// <summary>
    /// Optional friendly name for this provider (defaults to "Auth0").
    /// </summary>
    public string Name
    {
        get => string.IsNullOrWhiteSpace(_name) ? "Auth0" : _name;
        set => _name = value;
    }

    /// <summary>
    /// Optional role claim name (namespaced or otherwise). When set, values are mirrored into ClaimTypes.Role.
    /// </summary>
    public string? RoleClaimName { get; set; }

    /// <summary>
    /// Permission claim name to normalize (defaults to "permissions").
    /// </summary>
    public string PermissionClaimName { get; set; } = PrimusClaimTypes.Permission;

    /// <summary>
    /// Organization claim name to normalize (defaults to "org_id").
    /// </summary>
    public string OrganizationClaimName { get; set; } = PrimusClaimTypes.Organization;

    /// <summary>
    /// Enforce presence (and optionally a specific value) for the organization claim.
    /// </summary>
    public bool ValidateOrganization { get; set; }

    /// <summary>
    /// Optional required organization value when ValidateOrganization is enabled.
    /// </summary>
    public string? RequiredOrganization { get; set; }

    /// <summary>
    /// Allow Auth0 machine-to-machine (client-credentials) tokens.
    /// </summary>
    public bool AllowMachineToMachine { get; set; }

    /// <summary>
    /// Allowed grant types for M2M tokens (defaults to client-credentials when enabled).
    /// </summary>
    public List<string> AllowedGrantTypes { get; set; } = new();

    /// <summary>
    /// Allowed scopes for M2M tokens (if provided, token scopes must be within these).
    /// </summary>
    public List<string> AllowedMachineToMachineScopes { get; set; } = new();

    /// <summary>
    /// Require email_verified=true for user tokens.
    /// </summary>
    public bool RequireEmailVerification { get; set; }

    /// <summary>
    /// Claim mappings to normalize Auth0 claims into standard claim types.
    /// </summary>
    public Dictionary<string, string> ClaimMappings { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        { "sub", ClaimTypes.NameIdentifier },
        { "email", ClaimTypes.Email },
        { "name", ClaimTypes.Name }
    };

    internal IssuerConfig ToIssuerConfig()
    {
        var authority = BuildAuthority();
        var audiences = Audiences.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.Ordinal).ToList();

        if (!audiences.Any())
        {
            throw new ArgumentException("At least one audience is required for Auth0.", nameof(Audiences));
        }

        var issuerValue = authority.EndsWith("/", StringComparison.Ordinal) ? authority : $"{authority}/";

        return new IssuerConfig
        {
            Name = Name,
            Type = IssuerType.Auth0,
            Issuer = issuerValue,
            Authority = authority,
            Audiences = audiences,
            ClaimMappings = new Dictionary<string, string>(ClaimMappings, StringComparer.OrdinalIgnoreCase),
            RoleClaimName = string.IsNullOrWhiteSpace(RoleClaimName) ? null : RoleClaimName,
            PermissionClaimName = string.IsNullOrWhiteSpace(PermissionClaimName) ? PrimusClaimTypes.Permission : PermissionClaimName,
            OrganizationClaimName = string.IsNullOrWhiteSpace(OrganizationClaimName) ? PrimusClaimTypes.Organization : OrganizationClaimName,
            ValidateOrganization = ValidateOrganization,
            RequiredOrganization = RequiredOrganization,
            AllowMachineToMachine = AllowMachineToMachine,
            AllowedGrantTypes = NormalizeAllowedGrantTypes(),
            AllowedMachineToMachineScopes = NormalizeAllowedScopes(),
            RequireEmailVerification = RequireEmailVerification
        };
    }

    private string BuildAuthority()
    {
        if (string.IsNullOrWhiteSpace(Domain))
        {
            throw new ArgumentException("Auth0 domain is required.", nameof(Domain));
        }

        var trimmed = Domain.Trim().TrimEnd('/');
        if (!trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = $"https://{trimmed}";
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Auth0 domain must be a valid absolute URI or host.", nameof(Domain));
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Auth0 domain must use HTTPS.", nameof(Domain));
        }

        return uri.ToString().TrimEnd('/');
    }

    private List<string> NormalizeAllowedGrantTypes()
    {
        var list = AllowedGrantTypes?.Where(g => !string.IsNullOrWhiteSpace(g)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        if (AllowMachineToMachine && list.Count == 0)
        {
            list.Add("client-credentials");
        }
        return list;
    }

    private List<string> NormalizeAllowedScopes()
    {
        return AllowedMachineToMachineScopes?
                   .Where(s => !string.IsNullOrWhiteSpace(s))
                   .Select(s => s.Trim())
                   .Distinct(StringComparer.Ordinal)
                   .ToList() ?? new List<string>();
    }
}

/// <summary>
/// Convenience extensions for adding Auth0 as a provider without changing the main signature.
/// </summary>
public static class Auth0PrimusIdentityExtensions
{
    /// <summary>
    /// Adds Auth0 using a simple domain + audience signature with optional overrides.
    /// </summary>
    public static PrimusIdentityOptions UseAuth0(
        this PrimusIdentityOptions options,
        string domain,
        string audience,
        Action<Auth0Options>? configure = null)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        var auth0Options = new Auth0Options
        {
            Domain = domain
        };

        if (!string.IsNullOrWhiteSpace(audience))
        {
            auth0Options.Audiences.Add(audience);
        }

        configure?.Invoke(auth0Options);

        options.Issuers.Add(auth0Options.ToIssuerConfig());
        return options;
    }

    /// <summary>
    /// Adds Auth0 using the provider-style API.
    /// </summary>
    public static PrimusIdentityOptions AddProvider(
        this PrimusIdentityOptions options,
        IdentityProvider provider,
        Action<Auth0Options> configure)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (provider != IdentityProvider.Auth0) throw new ArgumentException("This overload is only for Auth0 providers.", nameof(provider));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var auth0Options = new Auth0Options();
        configure(auth0Options);

        options.Issuers.Add(auth0Options.ToIssuerConfig());
        return options;
    }
}

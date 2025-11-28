using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Configuration options for AWS Cognito user pools.
/// </summary>
public class CognitoOptions
{
    /// <summary>
    /// AWS region (e.g., us-east-1).
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Cognito User Pool ID (e.g., us-east-1_ABC123).
    /// </summary>
    public string UserPoolId { get; set; } = string.Empty;

    /// <summary>
    /// Allowed audiences (e.g., your app client IDs).
    /// </summary>
    public List<string> Audiences { get; set; } = new();

    /// <summary>
    /// Optional friendly name (defaults to "Cognito").
    /// </summary>
    public string Name { get; set; } = "Cognito";

    /// <summary>
    /// Optional claim mappings to normalize Cognito claims into standard claim types.
    /// </summary>
    public Dictionary<string, string> ClaimMappings { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        { "sub", ClaimTypes.NameIdentifier },
        { "email", ClaimTypes.Email },
        { "name", ClaimTypes.Name }
    };

    /// <summary>
    /// Optional role claim name (mapped into ClaimTypes.Role when provided).
    /// </summary>
    public string? RoleClaimName { get; set; } = "cognito:groups";

    internal IssuerConfig ToIssuerConfig()
    {
        if (string.IsNullOrWhiteSpace(Region))
        {
            throw new ArgumentException("Region is required.", nameof(Region));
        }
        if (string.IsNullOrWhiteSpace(UserPoolId))
        {
            throw new ArgumentException("UserPoolId is required.", nameof(UserPoolId));
        }

        var issuer = $"https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}";
        var audiences = Audiences.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.Ordinal).ToList();
        if (!audiences.Any())
        {
            throw new ArgumentException("At least one audience (app client ID) is required.", nameof(Audiences));
        }

        return new IssuerConfig
        {
            Name = string.IsNullOrWhiteSpace(Name) ? "Cognito" : Name,
            Type = IssuerType.Cognito,
            Issuer = issuer,
            Authority = issuer,
            Audiences = audiences,
            ClaimMappings = new Dictionary<string, string>(ClaimMappings, StringComparer.OrdinalIgnoreCase),
            RoleClaimName = string.IsNullOrWhiteSpace(RoleClaimName) ? null : RoleClaimName
        };
    }
}

public static class CognitoPrimusIdentityExtensions
{
    /// <summary>
    /// Adds AWS Cognito using region + user pool with OIDC discovery.
    /// </summary>
    public static PrimusIdentityOptions UseCognito(this PrimusIdentityOptions options, string region, string userPoolId, string audience, Action<CognitoOptions>? configure = null)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        var cognito = new CognitoOptions
        {
            Region = region,
            UserPoolId = userPoolId
        };
        if (!string.IsNullOrWhiteSpace(audience))
        {
            cognito.Audiences.Add(audience);
        }
        configure?.Invoke(cognito);
        options.Issuers.Add(cognito.ToIssuerConfig());
        return options;
    }

    /// <summary>
    /// Adds AWS Cognito using provider-style API.
    /// </summary>
    public static PrimusIdentityOptions AddProvider(this PrimusIdentityOptions options, IdentityProvider provider, Action<CognitoOptions> configure)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (provider != IdentityProvider.Cognito) throw new ArgumentException("This overload is only for Cognito providers.", nameof(provider));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var cognito = new CognitoOptions();
        configure(cognito);
        options.Issuers.Add(cognito.ToIssuerConfig());
        return options;
    }
}

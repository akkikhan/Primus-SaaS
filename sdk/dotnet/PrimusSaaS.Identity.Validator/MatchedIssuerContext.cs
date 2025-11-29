using Microsoft.AspNetCore.Http;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Captures which issuer configuration matched an incoming token.
/// </summary>
public sealed class MatchedIssuerContext
{
    public string Name { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public IssuerType Type { get; init; }
    public IReadOnlyList<string> Audiences { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Friendly provider name derived from issuer type.
    /// </summary>
    public string Provider
    {
        get
        {
            if (Type == IssuerType.Jwt) return "Custom JWT";
            if (Type == IssuerType.AzureAD) return "Azure AD / Entra ID";
            if (Type == IssuerType.Auth0) return "Auth0";
            if (Type == IssuerType.Google) return "Google";
            if (Type == IssuerType.Cognito) return "AWS Cognito";
            return Name;
        }
    }

    public static MatchedIssuerContext FromIssuerConfig(IssuerConfig config) =>
        new()
        {
            Name = config.Name,
            Issuer = config.Issuer,
            Type = config.Type,
            Audiences = config.Audiences.ToArray()
        };
}

/// <summary>
/// Helpers for accessing matched issuer metadata during a request.
/// </summary>
public static class PrimusIdentityContextExtensions
{
    internal const string MatchedIssuerItemKey = "Primus.Identity.MatchedIssuer";

    /// <summary>
    /// Gets the matched issuer information for the current request, if available.
    /// </summary>
    public static MatchedIssuerContext? GetMatchedIssuer(this HttpContext? context)
    {
        if (context?.Items == null) return null;

        if (context.Items.TryGetValue(MatchedIssuerItemKey, out var value) &&
            value is MatchedIssuerContext matched)
        {
            return matched;
        }

        return null;
    }
}

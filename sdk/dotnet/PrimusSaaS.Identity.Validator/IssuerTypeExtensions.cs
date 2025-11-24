namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Helper extensions for issuer type handling.
/// </summary>
internal static class IssuerTypeExtensions
{
    /// <summary>
    /// Returns true when the issuer uses OIDC flows (including AzureAD alias).
    /// </summary>
    public static bool IsOidcBased(this IssuerType type) =>
        type == IssuerType.Oidc || type == IssuerType.AzureAD;
}

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Standard claim type constants used by Primus Identity to normalize providers.
/// </summary>
public static class PrimusClaimTypes
{
    /// <summary>
    /// Normalized permission claim type.
    /// </summary>
    public const string Permission = "permissions";

    /// <summary>
    /// Normalized organization claim type.
    /// </summary>
    public const string Organization = "org_id";
}

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Friendly provider names for helper methods (does not change core IssuerConfig behavior).
/// </summary>
public enum IdentityProvider
{
    AzureAD,
    Auth0,
    Cognito,
    Google,
    Local
}

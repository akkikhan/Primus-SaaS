namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Interface for resolving tenant context from token claims.
/// Implement this interface to provide custom tenant resolution logic that can be tested and mocked.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves the tenant context from the provided token claims.
    /// </summary>
    /// <param name="claims">The claims extracted from the token.</param>
    /// <returns>The resolved tenant context, or null if tenant cannot be resolved.</returns>
    Task<TenantContext?> ResolveAsync(TokenClaims claims);
}

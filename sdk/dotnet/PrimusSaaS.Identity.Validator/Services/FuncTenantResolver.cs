namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Adapter to allow using a Func delegate as an ITenantResolver.
/// </summary>
internal class FuncTenantResolver : ITenantResolver
{
    private readonly Func<TokenClaims, TenantContext?> _resolverFunc;

    public FuncTenantResolver(Func<TokenClaims, TenantContext?> resolverFunc)
    {
        _resolverFunc = resolverFunc ?? throw new ArgumentNullException(nameof(resolverFunc));
    }

    public Task<TenantContext?> ResolveAsync(TokenClaims claims)
    {
        return Task.FromResult(_resolverFunc(claims));
    }
}

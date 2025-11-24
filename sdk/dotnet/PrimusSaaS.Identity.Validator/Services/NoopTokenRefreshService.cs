namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// No-op refresh service used when refresh is disabled.
/// </summary>
public class NoopTokenRefreshService : ITokenRefreshService
{
    public Task<string> IssueRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(string.Empty);

    public Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<TokenRefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        => Task.FromResult(TokenRefreshResult.Failed("Token refresh is disabled."));
}

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Interface for issuing and refreshing access tokens via refresh tokens.
/// </summary>
public interface ITokenRefreshService
{
    /// <summary>
    /// Issues a new refresh token for a user identifier.
    /// </summary>
    Task<string> IssueRefreshTokenAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a refresh token is still valid (exists and not expired/revoked).
    /// </summary>
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a refresh token for a new access token (and optionally a rotated refresh token).
    /// </summary>
    Task<TokenRefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}

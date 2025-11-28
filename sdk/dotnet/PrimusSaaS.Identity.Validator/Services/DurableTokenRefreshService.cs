using System.Security.Cryptography;
using System.Text;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Store-backed refresh token service with rotation and revocation support.
/// </summary>
public class DurableTokenRefreshService : ITokenRefreshService
{
    private readonly TokenRefreshOptions _options;
    private readonly IRefreshTokenStore _store;

    public DurableTokenRefreshService(TokenRefreshOptions options, IRefreshTokenStore store)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options.Validate();
    }

    public async Task<string> IssueRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId is required to issue a refresh token.", nameof(userId));
        }

        var refreshToken = GenerateSecureToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(_options.RefreshTokenTtl);
        await _store.StoreAsync(refreshToken, userId, expiresAt, cancellationToken);
        return refreshToken;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var record = await _store.GetAsync(refreshToken, cancellationToken);
        return record != null && !record.Revoked && record.ExpiresAt > DateTimeOffset.UtcNow;
    }

    public async Task<TokenRefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var record = await _store.GetAsync(refreshToken, cancellationToken);
        if (record == null || record.Revoked || record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return TokenRefreshResult.Failed("Invalid or expired refresh token.");
        }

        // Rotate refresh token
        await _store.RevokeAsync(refreshToken, cancellationToken);
        var newRefresh = await IssueRefreshTokenAsync(record.UserId, cancellationToken);

        var accessToken = GenerateSecureToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(_options.AccessTokenTtl);

        return new TokenRefreshResult
        {
            Success = true,
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            NewRefreshToken = newRefresh
        };
    }

    private static string GenerateSecureToken()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer.ToArray());
    }
}

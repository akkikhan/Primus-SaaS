using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Simple in-memory refresh token store for development/demo scenarios.
/// </summary>
public class InMemoryTokenRefreshService : ITokenRefreshService
{
    private readonly TokenRefreshOptions _options;
    private readonly ConcurrentDictionary<string, StoredToken> _store = new(StringComparer.Ordinal);
    private readonly object _cleanupLock = new();

    public InMemoryTokenRefreshService(TokenRefreshOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    public Task<string> IssueRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId is required to issue a refresh token.", nameof(userId));
        }

        var refreshToken = GenerateSecureToken();
        var now = DateTimeOffset.UtcNow;
        var stored = new StoredToken
        {
            UserId = userId,
            ExpiresAt = now.Add(_options.RefreshTokenTtl)
        };
        _store[refreshToken] = stored;
        return Task.FromResult(refreshToken);
    }

    public Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        CleanupExpired();
        if (_store.TryGetValue(refreshToken, out var stored))
        {
            if (stored.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public async Task<TokenRefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        CleanupExpired();
        if (!_store.TryGetValue(refreshToken, out var stored) || stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return TokenRefreshResult.Failed("Invalid or expired refresh token.");
        }

        var accessToken = GenerateSecureToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(_options.AccessTokenTtl);

        // Optional: rotate refresh token
        var newRefresh = await IssueRefreshTokenAsync(stored.UserId, cancellationToken);
        _store.TryRemove(refreshToken, out _);

        return new TokenRefreshResult
        {
            Success = true,
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            NewRefreshToken = newRefresh
        };
    }

    private void CleanupExpired()
    {
        // cheap best-effort cleanup
        if (!Monitor.TryEnter(_cleanupLock)) return;
        try
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var kvp in _store.ToArray())
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    _store.TryRemove(kvp.Key, out _);
                }
            }
        }
        finally
        {
            Monitor.Exit(_cleanupLock);
        }
    }

    private static string GenerateSecureToken()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer.ToArray());
    }

    private class StoredToken
    {
        public string UserId { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}

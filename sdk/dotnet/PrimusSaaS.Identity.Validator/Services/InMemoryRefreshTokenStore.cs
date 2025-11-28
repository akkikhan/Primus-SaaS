using System.Collections.Concurrent;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// In-memory refresh token store (non-durable; for tests/dev only).
/// </summary>
public class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _store = new(StringComparer.Ordinal);

    public Task StoreAsync(string refreshToken, string userId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        _store[refreshToken] = new RefreshTokenRecord
        {
            UserId = userId,
            ExpiresAt = expiresAt,
            Revoked = false
        };
        return Task.CompletedTask;
    }

    public Task<RefreshTokenRecord?> GetAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(refreshToken, out var record);
        return Task.FromResult(record);
    }

    public Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(refreshToken, out var record))
        {
            record.Revoked = true;
        }
        return Task.CompletedTask;
    }
}

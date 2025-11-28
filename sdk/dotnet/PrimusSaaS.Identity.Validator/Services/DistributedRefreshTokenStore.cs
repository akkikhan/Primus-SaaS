using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Refresh token store backed by IDistributedCache (e.g., Redis, SQL).
/// </summary>
public class DistributedRefreshTokenStore : IRefreshTokenStore
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _serializerOptions;

    public DistributedRefreshTokenStore(IDistributedCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task StoreAsync(string refreshToken, string userId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var record = new RefreshTokenRecord
        {
            UserId = userId,
            ExpiresAt = expiresAt,
            Revoked = false
        };

        var json = JsonSerializer.Serialize(record, _serializerOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt
        };
        await _cache.SetStringAsync(refreshToken, json, options, cancellationToken);
    }

    public async Task<RefreshTokenRecord?> GetAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var json = await _cache.GetStringAsync(refreshToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<RefreshTokenRecord>(json, _serializerOptions);
    }

    public Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Best-effort delete; if cache backend lacks delete, it will expire naturally.
        return _cache.RemoveAsync(refreshToken, cancellationToken);
    }
}

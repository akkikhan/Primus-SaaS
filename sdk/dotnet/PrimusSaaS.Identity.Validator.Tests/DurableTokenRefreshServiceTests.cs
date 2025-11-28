using FluentAssertions;
using PrimusSaaS.Identity.Validator.Services;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class DurableTokenRefreshServiceTests
{
    [Fact]
    public async Task RefreshAsync_RotatesTokenAndRevokesOld()
    {
        var options = new TokenRefreshOptions
        {
            Enabled = true,
            UseDurableStore = true,
            AccessTokenTtl = TimeSpan.FromMinutes(5),
            RefreshTokenTtl = TimeSpan.FromMinutes(30)
        };
        var store = new InMemoryRefreshTokenStore();
        var service = new DurableTokenRefreshService(options, store);

        var refresh = await service.IssueRefreshTokenAsync("user-1");

        var result = await service.RefreshAsync(refresh);

        result.Success.Should().BeTrue();
        result.NewRefreshToken.Should().NotBeNullOrEmpty();
        result.AccessToken.Should().NotBeNullOrEmpty();

        // Old token should now be revoked/invalid
        var validOld = await service.ValidateRefreshTokenAsync(refresh);
        validOld.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ReturnsFalse_WhenExpired()
    {
        var options = new TokenRefreshOptions
        {
            Enabled = true,
            UseDurableStore = true,
            AccessTokenTtl = TimeSpan.FromMinutes(5),
            RefreshTokenTtl = TimeSpan.FromMilliseconds(10)
        };
        var store = new InMemoryRefreshTokenStore();
        var service = new DurableTokenRefreshService(options, store);

        var refresh = await service.IssueRefreshTokenAsync("user-1");
        await Task.Delay(20);

        var valid = await service.ValidateRefreshTokenAsync(refresh);
        valid.Should().BeFalse();
    }

    [Fact]
    public async Task DistributedStore_WorksWithMemoryCache()
    {
        var options = new TokenRefreshOptions
        {
            Enabled = true,
            UseDurableStore = true,
            AccessTokenTtl = TimeSpan.FromMinutes(5),
            RefreshTokenTtl = TimeSpan.FromMinutes(30)
        };

        var cache = new FakeDistributedCache();
        var store = new DistributedRefreshTokenStore(cache);
        var service = new DurableTokenRefreshService(options, store);

        var refresh = await service.IssueRefreshTokenAsync("user-1");
        var valid = await service.ValidateRefreshTokenAsync(refresh);
        valid.Should().BeTrue();
    }

    private class FakeDistributedCache : Microsoft.Extensions.Caching.Distributed.IDistributedCache
    {
        private readonly Dictionary<string, (byte[] data, DateTimeOffset? expiry)> _store = new(StringComparer.Ordinal);

        public byte[]? Get(string key)
        {
            if (!_store.TryGetValue(key, out var entry)) return null;
            if (entry.expiry.HasValue && entry.expiry.Value <= DateTimeOffset.UtcNow) return null;
            return entry.data;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options)
        {
            DateTimeOffset? expiry = options.AbsoluteExpiration;
            if (!expiry.HasValue && options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                expiry = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }
            _store[key] = (value, expiry);
        }

        public Task SetAsync(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}

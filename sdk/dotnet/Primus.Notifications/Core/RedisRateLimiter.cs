using System;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;

namespace PrimusSaaS.Notifications.Core;

public sealed class RedisRateLimiter : IRateLimiter, IDisposable
{
    private readonly RedisRateLimitOptions _options;
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IDatabase _database;
    private bool _disposed;

    public RedisRateLimiter(IOptions<RedisRateLimitOptions> options)
    {
        _options = options?.Value ?? new RedisRateLimitOptions();
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("RedisRateLimitOptions.ConnectionString must be configured.");
        }

        _multiplexer = ConnectionMultiplexer.Connect(_options.ConnectionString);
        _database = _multiplexer.GetDatabase();
    }

    public bool TryConsume(Recipient recipient, RateLimitOptions options, out string reason)
    {
        reason = string.Empty;
        if (!options.Enabled)
        {
            return true;
        }

        var tenantKey = !string.IsNullOrWhiteSpace(recipient.TenantId)
            ? recipient.TenantId!
            : recipient.UserId ?? recipient.Email ?? recipient.PhoneNumber ?? "default";

        if (!TryConsumeInternal($"tenant:{tenantKey}", options.MaxPerWindow, options.WindowSeconds, out reason))
        {
            return false;
        }

        if (options.MaxPerRecipientPerWindow.HasValue && !string.IsNullOrWhiteSpace(recipient.Email))
        {
            if (!TryConsumeInternal($"recipient:{recipient.Email}", options.MaxPerRecipientPerWindow.Value, options.WindowSeconds, out reason))
            {
                return false;
            }
        }

        if (options.MaxPerRecipientPerWindow.HasValue && !string.IsNullOrWhiteSpace(recipient.PhoneNumber))
        {
            if (!TryConsumeInternal($"recipient:{recipient.PhoneNumber}", options.MaxPerRecipientPerWindow.Value, options.WindowSeconds, out reason))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryConsumeInternal(string scopeKey, int limit, int windowSeconds, out string reason)
    {
        var window = Math.Max(1, windowSeconds);
        var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / window;
        var redisKey = $"{_options.KeyPrefix}:{scopeKey}:{bucket}";

        var count = _database.StringIncrement(redisKey);
        if (count == 1)
        {
            var expirySeconds = window + Math.Max(0, _options.KeyExpiryPaddingSeconds);
            _database.KeyExpire(redisKey, TimeSpan.FromSeconds(expirySeconds));
        }

        if (count > limit)
        {
            reason = $"Rate limit exceeded for {scopeKey}";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _multiplexer.Dispose();
        _disposed = true;
    }
}

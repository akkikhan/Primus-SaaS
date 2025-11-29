using System.Collections.Concurrent;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;

namespace PrimusSaaS.Notifications.Core;

internal sealed class RateLimiter
{
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _counters = new();

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

        if (!ConsumeInternal($"tenant:{tenantKey}", options.MaxPerWindow, options.WindowSeconds, out reason))
        {
            return false;
        }

        if (options.MaxPerRecipientPerWindow.HasValue && !string.IsNullOrWhiteSpace(recipient.Email))
        {
            if (!ConsumeInternal($"recipient:{recipient.Email}", options.MaxPerRecipientPerWindow.Value, options.WindowSeconds, out reason))
            {
                return false;
            }
        }

        if (options.MaxPerRecipientPerWindow.HasValue && !string.IsNullOrWhiteSpace(recipient.PhoneNumber))
        {
            if (!ConsumeInternal($"recipient:{recipient.PhoneNumber}", options.MaxPerRecipientPerWindow.Value, options.WindowSeconds, out reason))
            {
                return false;
            }
        }

        return true;
    }

    private bool ConsumeInternal(string key, int limit, int windowSeconds, out string reason)
    {
        var counter = _counters.GetOrAdd(key, _ => new SlidingWindowCounter(windowSeconds));
        var allowed = counter.TryConsume(limit);
        reason = allowed ? string.Empty : $"Rate limit exceeded for {key}";
        return allowed;
    }
}

internal sealed class SlidingWindowCounter
{
    private readonly int _windowSeconds;
    private readonly ConcurrentQueue<DateTimeOffset> _timestamps = new();

    public SlidingWindowCounter(int windowSeconds)
    {
        _windowSeconds = Math.Max(1, windowSeconds);
    }

    public bool TryConsume(int limit)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddSeconds(-_windowSeconds);

        while (_timestamps.TryPeek(out var ts) && ts < windowStart)
        {
            _timestamps.TryDequeue(out _);
        }

        if (_timestamps.Count >= limit)
        {
            return false;
        }

        _timestamps.Enqueue(now);
        return true;
    }
}

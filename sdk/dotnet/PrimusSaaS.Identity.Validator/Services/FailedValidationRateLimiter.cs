using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Simple in-memory rate limiter for failed token validations (per remote IP with optional global ceiling).
/// </summary>
public class FailedValidationRateLimiter
{
    private readonly FailedValidationRateLimiterOptions _options;
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _buckets = new(StringComparer.OrdinalIgnoreCase);
    private const string GlobalKey = "__global__";

    public FailedValidationRateLimiter(FailedValidationRateLimiterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    /// <summary>
    /// Records a failed validation and returns true if the request should be limited.
    /// </summary>
    public bool RegisterFailure(HttpContext context)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        var key = GetClientKey(context) ?? GlobalKey;
        var now = DateTimeOffset.UtcNow;

        var clientCounter = _buckets.GetOrAdd(key, _ => new SlidingWindowCounter(_options.Window));
        var clientCount = clientCounter.Increment(now);

        if (clientCount > _options.MaxFailuresPerWindow)
        {
            return true;
        }

        if (_options.MaxGlobalFailuresPerWindow > 0)
        {
            var globalCounter = _buckets.GetOrAdd(GlobalKey, _ => new SlidingWindowCounter(_options.Window));
            var globalCount = globalCounter.Increment(now);
            if (globalCount > _options.MaxGlobalFailuresPerWindow)
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetClientKey(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ip) ? null : ip;
    }

    private sealed class SlidingWindowCounter
    {
        private readonly TimeSpan _window;
        private long _count;
        private DateTimeOffset _windowStart;
        private readonly object _lock = new();

        public SlidingWindowCounter(TimeSpan window)
        {
            _window = window;
            _windowStart = DateTimeOffset.UtcNow;
        }

        public long Increment(DateTimeOffset now)
        {
            lock (_lock)
            {
                if (now - _windowStart >= _window)
                {
                    _windowStart = now;
                    _count = 0;
                }

                _count++;
                return _count;
            }
        }
    }
}

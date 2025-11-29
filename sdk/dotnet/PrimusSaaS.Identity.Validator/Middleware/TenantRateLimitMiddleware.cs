using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Identity.Validator.Services;

namespace PrimusSaaS.Identity.Validator.Middleware;

/// <summary>
/// Enforces per-tenant/API key request rate limiting using a sliding window counter.
/// </summary>
public class TenantRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantRateLimitMiddleware> _logger;
    private readonly TenantRateLimitOptions _options;
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _counters = new(StringComparer.Ordinal);

    public TenantRateLimitMiddleware(RequestDelegate next, IOptions<PrimusIdentityOptions> options, ILogger<TenantRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _options = options.Value.TenantRateLimiting ?? new TenantRateLimitOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var key = ResolveTenantKey(context);
        if (string.IsNullOrWhiteSpace(key))
        {
            // if we cannot resolve a tenant, allow to avoid false positives
            await _next(context);
            return;
        }

        var counter = _counters.GetOrAdd(key, _ => new SlidingWindowCounter(_options.WindowSeconds));
        if (!counter.TryConsume(_options.MaxRequests))
        {
            _logger.LogWarning("Tenant rate limit exceeded for {TenantKey}", key);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = _options.WindowSeconds.ToString();
            await context.Response.WriteAsync("Rate limit exceeded.");
            return;
        }

        await _next(context);
    }

    private static string? ResolveTenantKey(HttpContext context)
    {
        var tenantContext = context.Items.ContainsKey("TenantContext") ? context.Items["TenantContext"] as TenantContext : null;
        if (tenantContext != null && !string.IsNullOrWhiteSpace(tenantContext.TenantId))
        {
            return tenantContext.TenantId;
        }

        var claim = context.User?.Claims.FirstOrDefault(c => c.Type == "tid" || c.Type == "tenant_id");
        if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
        {
            return claim.Value;
        }

        var apiKeyName = context.User?.Claims.FirstOrDefault(c => c.Type == "auth_type" && c.Value == "api_key");
        if (apiKeyName != null)
        {
            return context.User?.Identity?.Name ?? "api-key";
        }

        return null;
    }

    private sealed class SlidingWindowCounter
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
}

using PrimusSaaS.Identity.Validator.Models;
using System.Text.Json;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Service for fetching JWKS from Azure AD.
/// </summary>
public class JwksService
{
    private readonly HttpClient _httpClient;
    private readonly JwksCache? _cache;
    private readonly bool _enableCaching;
    private readonly JwksServiceOptions _options;
    private readonly SemaphoreSlim _fetchLock = new(1, 1);
    private long _cacheHits;
    private long _cacheMisses;
    private long _fetchAttempts;
    private long _fetchFailures;
    private DateTimeOffset? _lastSuccessUtc;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwksService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for fetching JWKS.</param>
    /// <param name="cache">JWKS cache.</param>
    /// <param name="enableCaching">Set false to bypass caching entirely (useful for tests/local).</param>
    /// <param name="options">Resiliency options for JWKS fetching.</param>
    public JwksService(HttpClient? httpClient = null, JwksCache? cache = null, bool enableCaching = true, JwksServiceOptions? options = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _enableCaching = enableCaching;
        _cache = enableCaching ? cache ?? new JwksCache() : null;
        _options = options ?? new JwksServiceOptions();
        _options.Validate();
    }

    /// <summary>
    /// Gets the JWKS from the specified URI, using cache if available.
    /// </summary>
    /// <param name="jwksUri">The JWKS endpoint URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JSON Web Key Set.</returns>
    public async Task<PrimusJsonWebKeySet> GetJwksAsync(
        string jwksUri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jwksUri))
        {
            throw new ArgumentException("JWKS URI cannot be null or empty.", nameof(jwksUri));
        }

        // Try cache first
        var cachedKeySet = _enableCaching ? _cache?.Get(jwksUri) : null;
        if (cachedKeySet != null)
        {
            Interlocked.Increment(ref _cacheHits);
            return cachedKeySet;
        }
        Interlocked.Increment(ref _cacheMisses);

        // Fetch from Azure AD
        await _fetchLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check cache after acquiring lock
            cachedKeySet = _enableCaching ? _cache?.Get(jwksUri) : null;
            if (cachedKeySet != null)
            {
                Interlocked.Increment(ref _cacheHits);
                return cachedKeySet;
            }

            var keySet = await FetchWithRetriesAsync(jwksUri, cancellationToken);

            if (_enableCaching && _cache != null)
            {
                _cache.Set(jwksUri, keySet);
            }

            return keySet;
        }
        finally
        {
            _fetchLock.Release();
        }
    }

    private async Task<PrimusJsonWebKeySet> FetchWithRetriesAsync(string jwksUri, CancellationToken cancellationToken)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                Interlocked.Increment(ref _fetchAttempts);
                var response = await _httpClient.GetAsync(jwksUri, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var keySet = JsonSerializer.Deserialize<PrimusJsonWebKeySet>(json);

                if (keySet == null)
                {
                    throw new InvalidOperationException("Failed to deserialize JWKS.");
                }

                _lastSuccessUtc = DateTimeOffset.UtcNow;
                return keySet;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException || ex is InvalidOperationException)
            {
                lastError = ex;
                Interlocked.Increment(ref _fetchFailures);
                if (attempt >= _options.MaxRetries)
                {
                    throw new HttpRequestException($"Failed to fetch JWKS from '{jwksUri}' after {attempt} attempts.", ex);
                }

                if (_options.BaseDelay > TimeSpan.Zero)
                {
                    var delay = TimeSpan.FromMilliseconds(_options.BaseDelay.TotalMilliseconds * attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        throw new HttpRequestException($"Failed to fetch JWKS from '{jwksUri}'.", lastError);
    }

    /// <summary>
    /// Returns a snapshot of JWKS diagnostics (thread-safe).
    /// </summary>
    public JwksServiceDiagnostics GetDiagnostics()
    {
        return new JwksServiceDiagnostics
        {
            CacheHits = Interlocked.Read(ref _cacheHits),
            CacheMisses = Interlocked.Read(ref _cacheMisses),
            FetchAttempts = Interlocked.Read(ref _fetchAttempts),
            FetchFailures = Interlocked.Read(ref _fetchFailures),
            LastSuccessUtc = _lastSuccessUtc
        };
    }

    /// <summary>
    /// Gets the JWKS for an Azure AD tenant by tenant ID.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JSON Web Key Set.</returns>
    public async Task<PrimusJsonWebKeySet> GetJwksForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var jwksUri = $"https://login.microsoftonline.com/{tenantId}/discovery/v2.0/keys";
        return await GetJwksAsync(jwksUri, cancellationToken);
    }

    /// <summary>
    /// Clears the JWKS cache.
    /// </summary>
    public void ClearCache()
    {
        _cache?.Clear();
    }
}

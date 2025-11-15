using PrimusSaaS.Identity.Validator.Models;
using System.Text.Json;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Service for fetching JWKS from Azure AD.
/// </summary>
public class JwksService
{
    private readonly HttpClient _httpClient;
    private readonly JwksCache _cache;
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="JwksService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for fetching JWKS.</param>
    /// <param name="cache">JWKS cache.</param>
    public JwksService(HttpClient? httpClient = null, JwksCache? cache = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _cache = cache ?? new JwksCache();
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
        // Try cache first
        var cachedKeySet = _cache.Get(jwksUri);
        if (cachedKeySet != null)
        {
            return cachedKeySet;
        }

        // Fetch from Azure AD
        await _fetchLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check cache after acquiring lock
            cachedKeySet = _cache.Get(jwksUri);
            if (cachedKeySet != null)
            {
                return cachedKeySet;
            }

            var response = await _httpClient.GetAsync(jwksUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var keySet = JsonSerializer.Deserialize<PrimusJsonWebKeySet>(json);

            if (keySet == null || keySet.Keys.Count == 0)
            {
                throw new InvalidOperationException("Failed to deserialize JWKS or no keys found.");
            }

            // Cache the key set
            _cache.Set(jwksUri, keySet);

            return keySet;
        }
        finally
        {
            _fetchLock.Release();
        }
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
        _cache.Clear();
    }
}

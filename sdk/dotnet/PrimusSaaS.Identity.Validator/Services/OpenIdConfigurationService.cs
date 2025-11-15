using PrimusSaaS.Identity.Validator.Models;
using System.Text.Json;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Service for fetching and caching OpenID Connect configuration.
/// </summary>
public class OpenIdConfigurationService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, CachedConfiguration> _configCache = new();
    private readonly TimeSpan _cacheTtl;
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenIdConfigurationService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for fetching configuration.</param>
    /// <param name="cacheTtl">Cache TTL for configuration. Defaults to 24 hours.</param>
    public OpenIdConfigurationService(HttpClient? httpClient = null, TimeSpan? cacheTtl = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _cacheTtl = cacheTtl ?? TimeSpan.FromHours(24);
    }

    /// <summary>
    /// Gets the OpenID Connect configuration for an Azure AD tenant.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The OpenID configuration.</returns>
    public async Task<PrimusOpenIdConfiguration> GetConfigurationAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var wellKnownUrl = GetWellKnownUrl(tenantId);

        // Check cache first
        if (_configCache.TryGetValue(wellKnownUrl, out var cached))
        {
            if (DateTimeOffset.UtcNow < cached.ExpiresAt)
            {
                return cached.Configuration;
            }

            // Expired - remove from cache
            _configCache.Remove(wellKnownUrl);
        }

        // Fetch from Azure AD
        await _fetchLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_configCache.TryGetValue(wellKnownUrl, out cached))
            {
                if (DateTimeOffset.UtcNow < cached.ExpiresAt)
                {
                    return cached.Configuration;
                }
            }

            var response = await _httpClient.GetAsync(wellKnownUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var configuration = JsonSerializer.Deserialize<PrimusOpenIdConfiguration>(json);

            if (configuration == null)
            {
                throw new InvalidOperationException("Failed to deserialize OpenID configuration.");
            }

            // Cache the configuration
            _configCache[wellKnownUrl] = new CachedConfiguration
            {
                Configuration = configuration,
                CachedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.Add(_cacheTtl)
            };

            return configuration;
        }
        finally
        {
            _fetchLock.Release();
        }
    }

    /// <summary>
    /// Gets the well-known configuration URL for an Azure AD tenant.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant ID.</param>
    /// <returns>The well-known URL.</returns>
    public static string GetWellKnownUrl(string tenantId)
    {
        return $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
    }

    /// <summary>
    /// Clears the configuration cache.
    /// </summary>
    public void ClearCache()
    {
        _configCache.Clear();
    }

    private class CachedConfiguration
    {
        public PrimusOpenIdConfiguration Configuration { get; set; } = null!;
        public DateTimeOffset CachedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}

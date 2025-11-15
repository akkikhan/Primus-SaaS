using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Identity.Validator.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// In-memory cache for JWKS (JSON Web Key Set) with TTL support.
/// </summary>
public class JwksCache
{
    private readonly ConcurrentDictionary<string, CachedJwks> _cache = new();
    private readonly TimeSpan _defaultTtl;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwksCache"/> class.
    /// </summary>
    /// <param name="defaultTtl">Default TTL for cached JWKS. Defaults to 24 hours.</param>
    public JwksCache(TimeSpan? defaultTtl = null)
    {
        _defaultTtl = defaultTtl ?? TimeSpan.FromHours(24);
    }

    /// <summary>
    /// Gets the JWKS from cache if available and not expired.
    /// </summary>
    /// <param name="cacheKey">The cache key (typically the JWKS URI).</param>
    /// <returns>The cached JWKS or null if not found or expired.</returns>
    public PrimusJsonWebKeySet? Get(string cacheKey)
    {
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            if (DateTimeOffset.UtcNow < cached.ExpiresAt)
            {
                return cached.KeySet;
            }

            // Expired - remove from cache
            _cache.TryRemove(cacheKey, out _);
        }

        return null;
    }

    /// <summary>
    /// Stores the JWKS in cache with the default TTL.
    /// </summary>
    /// <param name="cacheKey">The cache key (typically the JWKS URI).</param>
    /// <param name="keySet">The JWKS to cache.</param>
    public void Set(string cacheKey, PrimusJsonWebKeySet keySet)
    {
        Set(cacheKey, keySet, _defaultTtl);
    }

    /// <summary>
    /// Stores the JWKS in cache with a custom TTL.
    /// </summary>
    /// <param name="cacheKey">The cache key (typically the JWKS URI).</param>
    /// <param name="keySet">The JWKS to cache.</param>
    /// <param name="ttl">Time-to-live for this cache entry.</param>
    public void Set(string cacheKey, PrimusJsonWebKeySet keySet, TimeSpan ttl)
    {
        var cached = new CachedJwks
        {
            KeySet = keySet,
            CachedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl)
        };

        _cache.AddOrUpdate(cacheKey, cached, (_, __) => cached);
    }

    /// <summary>
    /// Converts a JSON Web Key to a SecurityKey for token validation.
    /// </summary>
    /// <param name="jwk">The JSON Web Key.</param>
    /// <returns>The security key or null if conversion fails.</returns>
    public static SecurityKey? ConvertToSecurityKey(PrimusJsonWebKey jwk)
    {
        if (jwk.KeyType != "RSA")
        {
            return null;
        }

        if (string.IsNullOrEmpty(jwk.Modulus) || string.IsNullOrEmpty(jwk.Exponent))
        {
            return null;
        }

        try
        {
            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlDecode(jwk.Modulus),
                Exponent = Base64UrlDecode(jwk.Exponent)
            });

            return new RsaSecurityKey(rsa)
            {
                KeyId = jwk.KeyId
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all security keys from the JWKS.
    /// </summary>
    /// <param name="keySet">The JWKS.</param>
    /// <returns>List of security keys.</returns>
    public static List<SecurityKey> GetSecurityKeys(PrimusJsonWebKeySet keySet)
    {
        var keys = new List<SecurityKey>();

        foreach (var jwk in keySet.Keys)
        {
            var securityKey = ConvertToSecurityKey(jwk);
            if (securityKey != null)
            {
                keys.Add(securityKey);
            }
        }

        return keys;
    }

    /// <summary>
    /// Clears all cached JWKS entries.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets the number of cached entries.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Decodes a Base64 URL-encoded string.
    /// </summary>
    private static byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }

    private class CachedJwks
    {
        public PrimusJsonWebKeySet KeySet { get; set; } = null!;
        public DateTimeOffset CachedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}

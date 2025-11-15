using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Identity.Validator.Models;
using PrimusSaaS.Identity.Validator.Services;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

/// <summary>
/// Tests for JwksCache - TTL-based caching with thread safety
/// </summary>
public class JwksCacheTests : IDisposable
{
    private readonly JwksCache _cache;
    private readonly RSA _rsa;

    public JwksCacheTests()
    {
        _cache = new JwksCache(TimeSpan.FromHours(1));
        _rsa = RSA.Create(2048);
    }

    [Fact]
    public void Get_WithKeyNotInCache_ReturnsNull()
    {
        // Act
        var result = _cache.Get("https://test.com/keys");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Set_AndGet_StoresAndRetrievesKeySet()
    {
        // Arrange
        var cacheKey = "https://test.com/keys";
        var keySet = CreateTestKeySet("test-kid-1");

        // Act
        _cache.Set(cacheKey, keySet);
        var result = _cache.Get(cacheKey);

        // Assert
        result.Should().NotBeNull();
        result!.Keys.Should().HaveCount(1);
        result.Keys[0].KeyId.Should().Be("test-kid-1");
    }

    [Fact]
    public async Task Get_WithExpiredCache_ReturnsNull()
    {
        // Arrange
        var shortTtlCache = new JwksCache(TimeSpan.FromMilliseconds(50));
        var cacheKey = "https://test.com/keys";
        var keySet = CreateTestKeySet("test-kid");

        // Act
        shortTtlCache.Set(cacheKey, keySet);
        await Task.Delay(100); // Wait for expiry
        var result = shortTtlCache.Get(cacheKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Set_WithCustomTtl_RespectsCustomTtl()
    {
        // Arrange
        var cacheKey = "https://test.com/keys";
        var keySet = CreateTestKeySet("test-kid");
        var customTtl = TimeSpan.FromSeconds(10);

        // Act
        _cache.Set(cacheKey, keySet, customTtl);
        var result = _cache.Get(cacheKey);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Clear_RemovesAllCachedEntries()
    {
        // Arrange
        _cache.Set("https://test1.com/keys", CreateTestKeySet("kid-1"));
        _cache.Set("https://test2.com/keys", CreateTestKeySet("kid-2"));
        _cache.Count.Should().Be(2);

        // Act
        _cache.Clear();

        // Assert
        _cache.Count.Should().Be(0);
        _cache.Get("https://test1.com/keys").Should().BeNull();
        _cache.Get("https://test2.com/keys").Should().BeNull();
    }

    [Fact]
    public void Set_UpdatingExistingKey_ReplacesOldValue()
    {
        // Arrange
        var cacheKey = "https://test.com/keys";
        var keySet1 = CreateTestKeySet("kid-1");
        var keySet2 = CreateTestKeySet("kid-2");

        // Act
        _cache.Set(cacheKey, keySet1);
        _cache.Set(cacheKey, keySet2); // Update

        var result = _cache.Get(cacheKey);

        // Assert
        result.Should().NotBeNull();
        result!.Keys.Should().HaveCount(1);
        result.Keys[0].KeyId.Should().Be("kid-2");
    }

    [Fact]
    public void Count_ReturnsCorrectNumberOfCachedEntries()
    {
        // Arrange & Act
        _cache.Set("https://test1.com/keys", CreateTestKeySet("kid-1"));
        _cache.Set("https://test2.com/keys", CreateTestKeySet("kid-2"));
        _cache.Set("https://test3.com/keys", CreateTestKeySet("kid-3"));

        // Assert
        _cache.Count.Should().Be(3);
    }

    [Theory]
    [InlineData(0.001)] // 3.6 seconds
    [InlineData(1)]     // 1 hour
    [InlineData(24)]    // 1 day
    [InlineData(168)]   // 1 week
    public void Constructor_WithVariousTtlValues_WorksCorrectly(double hours)
    {
        // Arrange & Act
        var cache = new JwksCache(TimeSpan.FromHours(hours));
        var cacheKey = "https://test.com/keys";
        var keySet = CreateTestKeySet("kid");

        cache.Set(cacheKey, keySet);
        var result = cache.Get(cacheKey);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ConvertToSecurityKey_WithValidRsaKey_ReturnsRsaSecurityKey()
    {
        // Arrange
        var parameters = _rsa.ExportParameters(false);
        var jwk = new PrimusJsonWebKey
        {
            KeyType = "RSA",
            KeyId = "test-kid",
            Modulus = Base64UrlEncode(parameters.Modulus!),
            Exponent = Base64UrlEncode(parameters.Exponent!)
        };

        // Act
        var securityKey = JwksCache.ConvertToSecurityKey(jwk);

        // Assert
        securityKey.Should().NotBeNull();
        securityKey.Should().BeOfType<RsaSecurityKey>();
        securityKey!.KeyId.Should().Be("test-kid");
    }

    [Fact]
    public void ConvertToSecurityKey_WithNonRsaKey_ReturnsNull()
    {
        // Arrange
        var jwk = new PrimusJsonWebKey
        {
            KeyType = "EC", // Elliptic Curve, not RSA
            KeyId = "test-kid"
        };

        // Act
        var securityKey = JwksCache.ConvertToSecurityKey(jwk);

        // Assert
        securityKey.Should().BeNull();
    }

    [Fact]
    public void ConvertToSecurityKey_WithMissingModulus_ReturnsNull()
    {
        // Arrange
        var jwk = new PrimusJsonWebKey
        {
            KeyType = "RSA",
            KeyId = "test-kid",
            Exponent = "AQAB" // Missing modulus
        };

        // Act
        var securityKey = JwksCache.ConvertToSecurityKey(jwk);

        // Assert
        securityKey.Should().BeNull();
    }

    [Fact]
    public void ConvertToSecurityKey_WithMissingExponent_ReturnsNull()
    {
        // Arrange
        var jwk = new PrimusJsonWebKey
        {
            KeyType = "RSA",
            KeyId = "test-kid",
            Modulus = "test-modulus" // Missing exponent
        };

        // Act
        var securityKey = JwksCache.ConvertToSecurityKey(jwk);

        // Assert
        securityKey.Should().BeNull();
    }

    [Fact]
    public void GetSecurityKeys_WithValidKeySet_ReturnsAllKeys()
    {
        // Arrange
        var parameters = _rsa.ExportParameters(false);
        var keySet = new PrimusJsonWebKeySet
        {
            Keys = new List<PrimusJsonWebKey>
            {
                new()
                {
                    KeyType = "RSA",
                    KeyId = "kid-1",
                    Modulus = Base64UrlEncode(parameters.Modulus!),
                    Exponent = Base64UrlEncode(parameters.Exponent!)
                },
                new()
                {
                    KeyType = "RSA",
                    KeyId = "kid-2",
                    Modulus = Base64UrlEncode(parameters.Modulus!),
                    Exponent = Base64UrlEncode(parameters.Exponent!)
                }
            }
        };

        // Act
        var securityKeys = JwksCache.GetSecurityKeys(keySet);

        // Assert
        securityKeys.Should().HaveCount(2);
        securityKeys[0].KeyId.Should().Be("kid-1");
        securityKeys[1].KeyId.Should().Be("kid-2");
    }

    [Fact]
    public void GetSecurityKeys_WithMixedValidAndInvalidKeys_ReturnsOnlyValidKeys()
    {
        // Arrange
        var parameters = _rsa.ExportParameters(false);
        var keySet = new PrimusJsonWebKeySet
        {
            Keys = new List<PrimusJsonWebKey>
            {
                new() // Valid RSA key
                {
                    KeyType = "RSA",
                    KeyId = "kid-1",
                    Modulus = Base64UrlEncode(parameters.Modulus!),
                    Exponent = Base64UrlEncode(parameters.Exponent!)
                },
                new() // Invalid - EC key
                {
                    KeyType = "EC",
                    KeyId = "kid-2"
                },
                new() // Invalid - missing modulus
                {
                    KeyType = "RSA",
                    KeyId = "kid-3",
                    Exponent = "AQAB"
                }
            }
        };

        // Act
        var securityKeys = JwksCache.GetSecurityKeys(keySet);

        // Assert
        securityKeys.Should().HaveCount(1);
        securityKeys[0].KeyId.Should().Be("kid-1");
    }

    [Fact]
    public void GetSecurityKeys_WithEmptyKeySet_ReturnsEmptyList()
    {
        // Arrange
        var keySet = new PrimusJsonWebKeySet { Keys = new List<PrimusJsonWebKey>() };

        // Act
        var securityKeys = JwksCache.GetSecurityKeys(keySet);

        // Assert
        securityKeys.Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentReads_AreThreadSafe()
    {
        // Arrange
        var cacheKey = "https://test.com/keys";
        var keySet = CreateTestKeySet("kid");
        _cache.Set(cacheKey, keySet);

        // Act - Multiple concurrent reads
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _cache.Get(cacheKey)))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All reads should succeed
        tasks.Should().AllSatisfy(task =>
        {
            task.Result.Should().NotBeNull();
            task.Result!.Keys[0].KeyId.Should().Be("kid");
        });
    }

    [Fact]
    public async Task ConcurrentWrites_AreThreadSafe()
    {
        // Arrange
        var cacheKey = "https://test.com/keys";

        // Act - Multiple concurrent writes
        var tasks = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => _cache.Set(cacheKey, CreateTestKeySet($"kid-{i}"))))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - Cache should have one entry (last write wins)
        _cache.Count.Should().Be(1);
        var result = _cache.Get(cacheKey);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ConcurrentWritesToDifferentKeys_AreThreadSafe()
    {
        // Arrange & Act - Multiple concurrent writes to different keys
        var tasks = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => _cache.Set($"https://test{i}.com/keys", CreateTestKeySet($"kid-{i}"))))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All keys should be cached
        _cache.Count.Should().Be(50);
    }

    [Fact]
    public async Task MixedConcurrentReadsAndWrites_RemainsConsistent()
    {
        // Arrange
        var cacheKey = "https://test.com/keys";
        _cache.Set(cacheKey, CreateTestKeySet("initial-kid"));

        // Act - Mix of reads and writes
        var readTasks = Enumerable.Range(0, 25)
            .Select(_ => Task.Run(() => _cache.Get(cacheKey)))
            .ToArray();

        var writeTasks = Enumerable.Range(0, 25)
            .Select(i => Task.Run(() => _cache.Set(cacheKey, CreateTestKeySet($"kid-{i}"))))
            .ToArray();

        await Task.WhenAll(readTasks.Concat(writeTasks));

        // Assert - No exceptions, cache has one entry
        _cache.Count.Should().Be(1);
        var result = _cache.Get(cacheKey);
        result.Should().NotBeNull();
    }

    private PrimusJsonWebKeySet CreateTestKeySet(string keyId)
    {
        var parameters = _rsa.ExportParameters(false);
        return new PrimusJsonWebKeySet
        {
            Keys = new List<PrimusJsonWebKey>
            {
                new()
                {
                    KeyType = "RSA",
                    KeyId = keyId,
                    Modulus = Base64UrlEncode(parameters.Modulus!),
                    Exponent = Base64UrlEncode(parameters.Exponent!),
                    Algorithm = "RS256",
                    Use = "sig"
                }
            }
        };
    }

    private static string Base64UrlEncode(byte[] data)
    {
        var base64 = Convert.ToBase64String(data);
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public void Dispose()
    {
        _rsa?.Dispose();
        GC.SuppressFinalize(this);
    }
}

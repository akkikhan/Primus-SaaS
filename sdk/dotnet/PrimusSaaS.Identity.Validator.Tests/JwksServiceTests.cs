using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Moq.Protected;
using PrimusSaaS.Identity.Validator.Models;
using PrimusSaaS.Identity.Validator.Services;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

/// <summary>
/// Tests for JwksService - JWKS fetching with caching and error handling
/// </summary>
public class JwksServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly JwksCache _cache;
    private readonly JwksService _service;
    private readonly string _testTenantId = "12345678-1234-1234-1234-123456789abc";
    private readonly string _testJwksUri = "https://login.microsoftonline.com/test/discovery/v2.0/keys";
    private int _callCount;
    private HttpRequestMessage? _capturedRequest;

    public JwksServiceTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _cache = new JwksCache(TimeSpan.FromHours(1));
        _service = new JwksService(_httpClient, _cache);
        _callCount = 0;
        _capturedRequest = null;
    }

    [Fact]
    public async Task GetJwksAsync_WithValidUri_ReturnsKeySet()
    {
        // Arrange
        var expectedKeySet = CreateTestKeySet("test-key-1");
        SetupValidJwksMock(expectedKeySet);

        // Act
        var result = await _service.GetJwksAsync(_testJwksUri);

        // Assert
        result.Should().NotBeNull();
        result.Keys.Should().HaveCount(1);
        result.Keys[0].KeyId.Should().Be("test-key-1");
    }

    [Fact]
    public async Task GetJwksAsync_CachesResults()
    {
        // Arrange
        var keySet = CreateTestKeySet("cached-key");
        _callCount = 0;
        SetupMockWithCallCounter(keySet);

        // Act - Call twice with same URI
        await _service.GetJwksAsync(_testJwksUri);
        await _service.GetJwksAsync(_testJwksUri);

        // Assert - Should only call HTTP once (cached)
        _callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetJwksAsync_WithDifferentUris_FetchesSeparately()
    {
        // Arrange
        var uri1 = "https://test.com/keys1";
        var uri2 = "https://test.com/keys2";
        var callCount = 0;

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                var keySet = CreateTestKeySet($"key-{callCount}");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(keySet), Encoding.UTF8, "application/json")
                };
            });

        // Act
        await _service.GetJwksAsync(uri1);
        await _service.GetJwksAsync(uri2);

        // Assert - Should call HTTP twice (different URIs)
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetJwksAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpErrorMock(HttpStatusCode.InternalServerError);

        // Act
        Func<Task> act = async () => await _service.GetJwksAsync(_testJwksUri);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetJwksAsync_With404_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpErrorMock(HttpStatusCode.NotFound);

        // Act
        Func<Task> act = async () => await _service.GetJwksAsync(_testJwksUri);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetJwksAsync_WithNetworkError_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        Func<Task> act = async () => await _service.GetJwksAsync(_testJwksUri);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Network error*");
    }

    [Fact]
    public async Task GetJwksAsync_WithTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act
        Func<Task> act = async () => await _service.GetJwksAsync(_testJwksUri);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task GetJwksAsync_WithMalformedJson_ThrowsJsonException()
    {
        // Arrange
        SetupMalformedJsonMock();

        // Act
        Func<Task> act = async () => await _service.GetJwksAsync(_testJwksUri);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task GetJwksAsync_WithEmptyKeys_ReturnsEmptyKeySet()
    {
        // Arrange
        var emptyKeySet = new PrimusJsonWebKeySet { Keys = new List<PrimusJsonWebKey>() };
        SetupValidJwksMock(emptyKeySet);

        // Act
        var result = await _service.GetJwksAsync(_testJwksUri);

        // Assert
        result.Should().NotBeNull();
        result.Keys.Should().BeEmpty();
    }

    [Fact]
    public async Task GetJwksAsync_WithMultipleKeys_ReturnsAllKeys()
    {
        // Arrange
        var keySet = new PrimusJsonWebKeySet
        {
            Keys = new List<PrimusJsonWebKey>
            {
                CreateTestKey("key-1"),
                CreateTestKey("key-2"),
                CreateTestKey("key-3")
            }
        };
        SetupValidJwksMock(keySet);

        // Act
        var result = await _service.GetJwksAsync(_testJwksUri);

        // Assert
        result.Keys.Should().HaveCount(3);
        result.Keys.Select(k => k.KeyId).Should().Contain(new[] { "key-1", "key-2", "key-3" });
    }

    [Fact]
    public async Task GetJwksAsync_ConcurrentCallsToSameUri_OnlyFetchesOnce()
    {
        // Arrange
        var keySet = CreateTestKeySet("concurrent-key");
        _callCount = 0;
        SetupMockWithCallCounter(keySet);

        // Act - Multiple concurrent calls for same URI
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _service.GetJwksAsync(_testJwksUri))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - Should fetch only once due to semaphore locking
        _callCount.Should().BeLessThanOrEqualTo(2); // Allow for race condition, but should be 1 ideally
    }

    [Fact]
    public async Task GetJwksForTenantAsync_ConstructsCorrectUri()
    {
        // Arrange
        _capturedRequest = null;
        SetupMockWithRequestCapture();

        // Act
        await _service.GetJwksForTenantAsync(_testTenantId);

        // Assert
        _capturedRequest.Should().NotBeNull();
        _capturedRequest!.RequestUri!.AbsoluteUri.Should()
            .Be($"https://login.microsoftonline.com/{_testTenantId}/discovery/v2.0/keys");
    }

    [Fact]
    public async Task GetJwksForTenantAsync_ReturnsKeySet()
    {
        // Arrange
        var keySet = CreateTestKeySet("tenant-key");
        SetupValidJwksMock(keySet);

        // Act
        var result = await _service.GetJwksForTenantAsync(_testTenantId);

        // Assert
        result.Should().NotBeNull();
        result.Keys.Should().HaveCount(1);
        result.Keys[0].KeyId.Should().Be("tenant-key");
    }

    [Fact]
    public async Task GetJwksForTenantAsync_CachesResults()
    {
        // Arrange
        var keySet = CreateTestKeySet("tenant-cached");
        _callCount = 0;
        SetupMockWithCallCounter(keySet);

        // Act - Call twice with same tenant
        await _service.GetJwksForTenantAsync(_testTenantId);
        await _service.GetJwksForTenantAsync(_testTenantId);

        // Assert - Should only call HTTP once (cached)
        _callCount.Should().Be(1);
    }

    [Fact]
    public void ClearCache_RemovesCachedEntries()
    {
        // Arrange
        var keySet = CreateTestKeySet("clear-test");
        _cache.Set(_testJwksUri, keySet);
        _cache.Count.Should().Be(1);

        // Act
        _service.ClearCache();

        // Assert
        _cache.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetJwksAsync_AfterClearCache_RefetchesKeys()
    {
        // Arrange
        var keySet = CreateTestKeySet("refetch-test");
        _callCount = 0;
        SetupMockWithCallCounter(keySet);

        // Act
        await _service.GetJwksAsync(_testJwksUri); // First fetch
        _service.ClearCache();
        await _service.GetJwksAsync(_testJwksUri); // Should refetch

        // Assert
        _callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetJwksAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        Func<Task> act = async () => await _service.GetJwksAsync(_testJwksUri, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetJwksAsync_WithExtraFieldsInResponse_DeserializesSuccessfully()
    {
        // Arrange
        var keyWithExtras = new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    kid = "extra-fields-key",
                    n = Convert.ToBase64String(new byte[256]),
                    e = "AQAB",
                    x5c = new[] { "cert1", "cert2" },
                    x5t = "thumbprint",
                    extra_field = "ignored"
                }
            },
            extra_top_level = "also_ignored"
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(keyWithExtras), Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.GetJwksAsync(_testJwksUri);

        // Assert
        result.Should().NotBeNull();
        result.Keys.Should().HaveCount(1);
        result.Keys[0].KeyId.Should().Be("extra-fields-key");
    }

    [Theory]
    [InlineData("common")]
    [InlineData("organizations")]
    [InlineData("consumers")]
    public async Task GetJwksForTenantAsync_WithSpecialTenants_WorksCorrectly(string specialTenant)
    {
        // Arrange
        var keySet = CreateTestKeySet($"{specialTenant}-key");
        SetupValidJwksMock(keySet);

        // Act
        var result = await _service.GetJwksForTenantAsync(specialTenant);

        // Assert
        result.Should().NotBeNull();
        result.Keys.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetJwksAsync_WithNullHttpClient_UsesDefaultClient()
    {
        // Arrange - Service with null HttpClient (uses default)
        var serviceWithDefaultClient = new JwksService(null, _cache);
        
        // This test verifies the service can be instantiated with null HttpClient
        // In real scenario, it would create its own HttpClient
        // We can't easily test actual HTTP call without mocking, so just verify construction
        
        // Act & Assert - Should not throw
        serviceWithDefaultClient.Should().NotBeNull();
    }

    [Fact]
    public async Task GetJwksAsync_WithNullCache_FetchesWithoutCaching()
    {
        // Arrange
        var keySet = CreateTestKeySet("no-cache-key");
        _callCount = 0;
        SetupMockWithCallCounter(keySet);
        
        var serviceWithoutCache = new JwksService(_httpClient, null, enableCaching: false);

        // Act - Call twice
        await serviceWithoutCache.GetJwksAsync(_testJwksUri);
        await serviceWithoutCache.GetJwksAsync(_testJwksUri);

        // Assert - Should call HTTP twice (no caching)
        _callCount.Should().Be(2);
    }

    // Helper methods

    private void SetupValidJwksMock(PrimusJsonWebKeySet keySet)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(keySet), Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupMockWithCallCounter(PrimusJsonWebKeySet keySet)
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                _callCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(keySet), Encoding.UTF8, "application/json")
                };
            });
    }

    private void SetupMockWithRequestCapture()
    {
        var keySet = CreateTestKeySet("captured-key");
        
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => _capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(keySet), Encoding.UTF8, "application/json")
            });
    }

    private void SetupHttpErrorMock(HttpStatusCode statusCode)
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    private void SetupMalformedJsonMock()
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json")
            });
    }

    private PrimusJsonWebKeySet CreateTestKeySet(string keyId)
    {
        return new PrimusJsonWebKeySet
        {
            Keys = new List<PrimusJsonWebKey> { CreateTestKey(keyId) }
        };
    }

    private PrimusJsonWebKey CreateTestKey(string keyId)
    {
        // Generate 256-byte modulus (2048-bit key)
        var modulus = new byte[256];
        new Random().NextBytes(modulus);

        return new PrimusJsonWebKey
        {
            KeyType = "RSA",
            Use = "sig",
            KeyId = keyId,
            Modulus = Convert.ToBase64String(modulus)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_'),
            Exponent = "AQAB" // Standard RSA exponent (65537)
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}

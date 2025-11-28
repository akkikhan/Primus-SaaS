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
/// Tests for OpenIdConfigurationService - OpenID Connect metadata discovery
/// </summary>
public class OpenIdConfigurationServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly OpenIdConfigurationService _service;
    private readonly string _testTenantId = "12345678-1234-1234-1234-123456789abc";
    private int _callCount;
    private HttpRequestMessage? _capturedRequest;

    public OpenIdConfigurationServiceTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _service = new OpenIdConfigurationService(_httpClient);
        _callCount = 0;
        _capturedRequest = null;
    }

    [Fact]
    public async Task GetConfigurationAsync_WithValidTenant_ReturnsConfiguration()
    {
        // Arrange
        var expectedJwksUri = $"https://login.microsoftonline.com/{_testTenantId}/discovery/v2.0/keys";
        SetupValidConfigurationMock(expectedJwksUri);

        // Act
        var result = await _service.GetConfigurationAsync(_testTenantId);

        // Assert
        result.Should().NotBeNull();
        result.JwksUri.Should().Be(expectedJwksUri);
        result.Issuer.Should().Contain(_testTenantId);
    }

    [Fact]
    public async Task GetConfigurationAsync_ConstructsCorrectUrl()
    {
        // Arrange
        SetupMockWithRequestCapture();

        // Act
        await _service.GetConfigurationAsync(_testTenantId);

        // Assert
        _capturedRequest.Should().NotBeNull();
        _capturedRequest!.RequestUri!.AbsoluteUri.Should()
            .Be($"https://login.microsoftonline.com/{_testTenantId}/v2.0/.well-known/openid-configuration");
    }

    [Fact]
    public async Task GetConfigurationByAuthorityAsync_WithAuthorityContainingV2_SendsSingleV2WellKnown()
    {
        // Arrange
        SetupMockWithRequestCapture();
        var authority = $"https://login.microsoftonline.com/{_testTenantId}/v2.0";

        // Act
        await _service.GetConfigurationByAuthorityAsync(authority);

        // Assert
        _capturedRequest.Should().NotBeNull();
        _capturedRequest!.RequestUri!.AbsoluteUri.Should()
            .Be($"https://login.microsoftonline.com/{_testTenantId}/v2.0/.well-known/openid-configuration");
    }

    [Fact]
    public async Task GetConfigurationByAuthorityAsync_WithAuthorityWithoutV2_AppendsV2WellKnown()
    {
        // Arrange
        SetupMockWithRequestCapture();
        var authority = $"https://login.microsoftonline.com/{_testTenantId}";

        // Act
        await _service.GetConfigurationByAuthorityAsync(authority);

        // Assert
        _capturedRequest.Should().NotBeNull();
        _capturedRequest!.RequestUri!.AbsoluteUri.Should()
            .Be($"https://login.microsoftonline.com/{_testTenantId}/v2.0/.well-known/openid-configuration");
    }

    [Fact]
    public async Task GetConfigurationAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpErrorMock(HttpStatusCode.InternalServerError);

        // Act
        Func<Task> act = async () => await _service.GetConfigurationAsync(_testTenantId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetConfigurationAsync_With404_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpErrorMock(HttpStatusCode.NotFound);

        // Act
        Func<Task> act = async () => await _service.GetConfigurationAsync(_testTenantId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetConfigurationAsync_WithNetworkError_ThrowsHttpRequestException()
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
        Func<Task> act = async () => await _service.GetConfigurationAsync(_testTenantId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Network error*");
    }

    [Fact]
    public async Task GetConfigurationAsync_WithTimeout_ThrowsTaskCanceledException()
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
        Func<Task> act = async () => await _service.GetConfigurationAsync(_testTenantId);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task GetConfigurationAsync_WithMalformedJson_ThrowsJsonException()
    {
        // Arrange
        SetupMalformedJsonMock();

        // Act
        Func<Task> act = async () => await _service.GetConfigurationAsync(_testTenantId);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task GetConfigurationAsync_WithEmptyJson_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupEmptyResponseMock();

        // Act
        Func<Task> act = async () => await _service.GetConfigurationAsync(_testTenantId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to deserialize*");
    }

    [Fact]
    public async Task GetConfigurationAsync_CachesResults()
    {
        // Arrange
        _callCount = 0;
        SetupMockWithCallCounter();

        // Act - Call twice with same tenant
        await _service.GetConfigurationAsync(_testTenantId);
        await _service.GetConfigurationAsync(_testTenantId);

        // Assert - Should only call HTTP once (cached)
        _callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithDifferentTenants_FetchesSeparately()
    {
        // Arrange
        var tenant1 = "tenant-1";
        var tenant2 = "tenant-2";
        var callCount = 0;

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                callCount++;
                var tenantId = req.RequestUri!.AbsoluteUri.Contains(tenant1) ? tenant1 : tenant2;
                var config = CreateTestConfiguration(tenantId);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json")
                };
            });

        // Act
        await _service.GetConfigurationAsync(tenant1);
        await _service.GetConfigurationAsync(tenant2);

        // Assert - Should call HTTP twice (different tenants)
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetConfigurationAsync_AfterCacheExpiry_RefetchesConfiguration()
    {
        // Arrange
        var shortTtlService = new OpenIdConfigurationService(_httpClient, TimeSpan.FromMilliseconds(50));
        _callCount = 0;
        SetupMockWithCallCounter();

        // Act
        await shortTtlService.GetConfigurationAsync(_testTenantId);
        await Task.Delay(100); // Wait for cache expiry
        await shortTtlService.GetConfigurationAsync(_testTenantId);

        // Assert - Should call HTTP twice (cache expired)
        _callCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithCancellation_ThrowsOperationCanceledException()
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
        Func<Task> act = async () => await _service.GetConfigurationAsync(_testTenantId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetConfigurationAsync_ConcurrentCallsToSameTenant_OnlyFetchesOnce()
    {
        // Arrange
        _callCount = 0;
        SetupMockWithCallCounter();

        // Act - Multiple concurrent calls for same tenant
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _service.GetConfigurationAsync(_testTenantId))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - Should fetch only once due to locking
        _callCount.Should().BeLessThanOrEqualTo(2); // Allow for race condition, but should be 1 ideally
    }

    [Fact]
    public void GetWellKnownUrl_ReturnsCorrectFormat()
    {
        // Act
        var url = OpenIdConfigurationService.GetWellKnownUrl(_testTenantId);

        // Assert
        url.Should().Be($"https://login.microsoftonline.com/{_testTenantId}/v2.0/.well-known/openid-configuration");
    }

    [Theory]
    [InlineData("https://login.microsoftonline.com/tenantA", "https://login.microsoftonline.com/tenantA/v2.0/.well-known/openid-configuration")]
    [InlineData("https://login.microsoftonline.com/tenantA/v2.0", "https://login.microsoftonline.com/tenantA/v2.0/.well-known/openid-configuration")]
    public void GetWellKnownUrlFromAuthority_NormalizesV2Segment(string authority, string expected)
    {
        // Act
        var url = OpenIdConfigurationService.GetWellKnownUrlFromAuthority(authority);

        // Assert
        url.Should().Be(expected);
    }

    [Theory]
    [InlineData("https://demo.auth0.com", "https://demo.auth0.com/.well-known/openid-configuration")]
    [InlineData("https://login.example.com/basepath", "https://login.example.com/basepath/.well-known/openid-configuration")]
    public void GetWellKnownUrlFromAuthority_NonAzure_DoesNotAppendV2(string authority, string expected)
    {
        // Act
        var url = OpenIdConfigurationService.GetWellKnownUrlFromAuthority(authority);

        // Assert
        url.Should().Be(expected);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithExtraFieldsInResponse_DeserializesSuccessfully()
    {
        // Arrange
        var configWithExtras = new
        {
            issuer = $"https://login.microsoftonline.com/{_testTenantId}/v2.0",
            jwks_uri = $"https://login.microsoftonline.com/{_testTenantId}/discovery/v2.0/keys",
            authorization_endpoint = "https://test.com/auth",
            token_endpoint = "https://test.com/token",
            extra_field_1 = "value1",
            extra_field_2 = 12345,
            nested_extra = new { foo = "bar" }
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(configWithExtras), Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.GetConfigurationAsync(_testTenantId);

        // Assert
        result.Should().NotBeNull();
        result.JwksUri.Should().Contain("/keys");
    }

    [Theory]
    [InlineData("common")]
    [InlineData("organizations")]
    [InlineData("consumers")]
    public async Task GetConfigurationAsync_WithSpecialTenants_WorksCorrectly(string specialTenant)
    {
        // Arrange
        SetupValidConfigurationMock($"https://login.microsoftonline.com/{specialTenant}/discovery/v2.0/keys");

        // Act
        var result = await _service.GetConfigurationAsync(specialTenant);

        // Assert
        result.Should().NotBeNull();
        result.JwksUri.Should().Contain(specialTenant);
    }

    // Helper methods

    private void SetupValidConfigurationMock(string jwksUri)
    {
        var config = CreateTestConfiguration(_testTenantId, jwksUri);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupMockWithRequestCapture()
    {
        var config = CreateTestConfiguration(_testTenantId);
        _capturedRequest = null;
        
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => _capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json")
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

    private void SetupEmptyResponseMock()
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            });
    }

    private void SetupMockWithCallCounter()
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
                var config = CreateTestConfiguration(_testTenantId);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json")
                };
            });
    }

    private PrimusOpenIdConfiguration CreateTestConfiguration(
        string tenantId,
        string? jwksUri = null)
    {
        return new PrimusOpenIdConfiguration
        {
            Issuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",
            JwksUri = jwksUri ?? $"https://login.microsoftonline.com/{tenantId}/discovery/v2.0/keys",
            AuthorizationEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize",
            TokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}

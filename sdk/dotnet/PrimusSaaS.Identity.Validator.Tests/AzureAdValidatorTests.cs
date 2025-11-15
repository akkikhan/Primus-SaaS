using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.Protected;
using PrimusSaaS.Identity.Validator.Models;
using PrimusSaaS.Identity.Validator.Services;
using PrimusSaaS.Identity.Validator.Validators;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

/// <summary>
/// Tests for AzureAdValidator - Full Azure AD token validation
/// </summary>
public class AzureAdValidatorTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly OpenIdConfigurationService _configService;
    private readonly JwksCache _cache;
    private readonly JwksService _jwksService;
    private readonly AzureAdValidator _validator;
    private readonly RSA _rsa;
    private readonly string _testTenantId = "12345678-1234-1234-1234-123456789abc";
    private readonly string _testAudience = "api://test-app";
    private readonly string _testKeyId = "test-key-1";
    private int _callCount;

    public AzureAdValidatorTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _configService = new OpenIdConfigurationService(_httpClient);
        _cache = new JwksCache(TimeSpan.FromHours(1));
        _jwksService = new JwksService(_httpClient, _cache);
        _validator = new AzureAdValidator(_configService, _jwksService);
        _rsa = RSA.Create(2048);
        _callCount = 0;
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var token = CreateValidToken(_testTenantId, _testAudience);
        SetupValidAzureAdMocks();

        // Act
        var result = await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: false); // Skip lifetime for test

        // Assert
        result.Should().NotBeNull();
        result.Identity.Should().NotBeNull();
        result.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ExtractsCorrectClaims()
    {
        // Arrange
        var token = CreateValidToken(_testTenantId, _testAudience, userId: "user-123", email: "test@example.com");
        SetupValidAzureAdMocks();

        // Act
        var result = await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: false);

        // Assert
        result.FindFirst("sub")?.Value.Should().Be("user-123");
        result.FindFirst("email")?.Value.Should().Be("test@example.com");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ThrowsSecurityTokenException()
    {
        // Arrange
        var token = CreateExpiredToken(_testTenantId, _testAudience);
        SetupValidAzureAdMocks();

        // Act
        Func<Task> act = async () => await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: true); // Enable lifetime validation

        // Assert
        var exception = await act.Should().ThrowAsync<SecurityTokenValidationException>();
        exception.Which.InnerException.Should().BeOfType<SecurityTokenExpiredException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWrongTenant_ThrowsSecurityTokenException()
    {
        // Arrange
        var wrongTenantId = "wrong-tenant-id";
        var token = CreateValidToken(wrongTenantId, _testAudience);
        SetupValidAzureAdMocks();

        // Act
        Func<Task> act = async () => await _validator.ValidateTokenAsync(
            token,
            _testTenantId, // Expect different tenant
            _testAudience,
            validateLifetime: false);

        // Assert
        var exception = await act.Should().ThrowAsync<SecurityTokenValidationException>();
        exception.Which.InnerException.Should().BeOfType<SecurityTokenInvalidIssuerException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWrongAudience_ThrowsSecurityTokenException()
    {
        // Arrange
        var wrongAudience = "api://wrong-app";
        var token = CreateValidToken(_testTenantId, wrongAudience);
        SetupValidAzureAdMocks();

        // Act
        Func<Task> act = async () => await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience, // Expect different audience
            validateLifetime: false);

        // Assert
        var exception = await act.Should().ThrowAsync<SecurityTokenValidationException>();
        exception.Which.InnerException.Should().BeOfType<SecurityTokenInvalidAudienceException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidSignature_ThrowsSecurityTokenException()
    {
        // Arrange
        var differentRsa = RSA.Create(2048);
        var token = CreateTokenWithDifferentKey(differentRsa, _testTenantId, _testAudience);
        SetupValidAzureAdMocks(); // Mocks use _rsa, not differentRsa

        // Act
        Func<Task> act = async () => await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: false);

        // Assert
        var exception = await act.Should().ThrowAsync<SecurityTokenValidationException>();
        exception.Which.InnerException.Should().Match<Exception>(e => 
            e is SecurityTokenSignatureKeyNotFoundException || e is SecurityTokenInvalidSignatureException);

        differentRsa.Dispose();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithMalformedToken_ThrowsSecurityTokenException()
    {
        // Arrange
        var malformedToken = "not.a.valid.jwt.token";
        SetupValidAzureAdMocks();

        // Act
        Func<Task> act = async () => await _validator.ValidateTokenAsync(
            malformedToken,
            _testTenantId,
            _testAudience,
            validateLifetime: false);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithV1Endpoint_ValidatesCorrectly()
    {
        // Arrange
        var v1Issuer = $"https://sts.windows.net/{_testTenantId}/";
        var token = CreateValidToken(_testTenantId, _testAudience, issuer: v1Issuer);
        SetupAzureAdMocksWithV1Endpoint();

        // Act
        var result = await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: false);

        // Assert
        result.Should().NotBeNull();
        result.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithRoleClaims_ExtractsRoles()
    {
        // Arrange
        var roles = new[] { "Admin", "User" };
        var token = CreateValidToken(_testTenantId, _testAudience, roles: roles);
        SetupValidAzureAdMocks();

        // Act
        var result = await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: false);

        // Assert
        result.FindAll(ClaimTypes.Role).Select(c => c.Value).Should().Contain(roles);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithCustomClockSkew_AppliesSkew()
    {
        // Arrange
        var token = CreateTokenExpiringIn(TimeSpan.FromSeconds(30)); // Expires in 30 seconds
        SetupValidAzureAdMocks();

        // Act - With large clock skew, should still validate
        var result = await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: true,
            clockSkew: TimeSpan.FromMinutes(5)); // 5 minute skew

        // Assert
        result.Should().NotBeNull();
        result.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithMissingKid_ValidatesSuccessfully()
    {
        // Arrange
        // When kid is missing, JWT validator tries all keys in JWKS
        // Since we're using the same RSA key for both token and JWKS, it validates successfully
        var token = CreateTokenWithoutKeyId(_testTenantId, _testAudience);
        SetupValidAzureAdMocks();

        // Act
        var result = await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: false);

        // Assert
        result.Should().NotBeNull();
        result.Identity?.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var token = CreateValidToken(_testTenantId, _testAudience);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        SetupMockToThrowOperationCanceled();

        // Act
        Func<Task> act = async () => await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: false,
            cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task TryValidateTokenAsync_WithValidToken_ReturnsSuccessResult()
    {
        // Arrange
        var token = CreateValidToken(_testTenantId, _testAudience);
        SetupValidAzureAdMocks();

        // Act
        var result = await _validator.TryValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: false);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task TryValidateTokenAsync_WithInvalidToken_ReturnsFailureResult()
    {
        // Arrange
        var token = CreateValidToken(_testTenantId, "wrong-audience");
        SetupValidAzureAdMocks();

        // Act
        var result = await _validator.TryValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience, // Expect different audience
            validateLifetime: false);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Principal.Should().BeNull();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TryValidateTokenAsync_WithExpiredToken_ReturnsFailureWithException()
    {
        // Arrange
        var token = CreateExpiredToken(_testTenantId, _testAudience);
        SetupValidAzureAdMocks();

        // Act
        var result = await _validator.TryValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: true);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("common")]
    [InlineData("organizations")]
    [InlineData("consumers")]
    public async Task ValidateTokenAsync_WithSpecialTenants_ValidatesCorrectly(string specialTenant)
    {
        // Arrange
        var token = CreateValidToken(specialTenant, _testAudience);
        SetupAzureAdMocksForTenant(specialTenant);

        // Act
        var result = await _validator.ValidateTokenAsync(
            token,
            specialTenant,
            _testAudience,
            validateLifetime: false);

        // Assert
        result.Should().NotBeNull();
        result.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithHttpError_ThrowsException()
    {
        // Arrange
        var token = CreateValidToken(_testTenantId, _testAudience);
        SetupHttpErrorMock(HttpStatusCode.InternalServerError);

        // Act
        Func<Task> act = async () => await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: false);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_CachesJwksKeys()
    {
        // Arrange
        var token1 = CreateValidToken(_testTenantId, _testAudience);
        var token2 = CreateValidToken(_testTenantId, _testAudience);
        _callCount = 0;
        SetupMockWithCallCounter();

        // Act - Validate two tokens
        await _validator.ValidateTokenAsync(token1, _testTenantId, _testAudience, validateLifetime: false);
        await _validator.ValidateTokenAsync(token2, _testTenantId, _testAudience, validateLifetime: false);

        // Assert - Should fetch config/JWKS only once (cached)
        _callCount.Should().BeLessThanOrEqualTo(2); // Config + JWKS, both cached
    }

    [Fact]
    public async Task ValidateTokenAsync_WithTidClaim_ValidatesTenant()
    {
        // Arrange
        var token = CreateValidToken(_testTenantId, _testAudience, includeTidClaim: true);
        SetupValidAzureAdMocks();

        // Act
        var result = await _validator.ValidateTokenAsync(
            token,
            _testTenantId,
            _testAudience,
            validateLifetime: false);

        // Assert
        result.FindFirst("tid")?.Value.Should().Be(_testTenantId);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWrongTidClaim_ThrowsException()
    {
        // Arrange
        var wrongTenant = "wrong-tenant-id";
        var token = CreateValidToken(wrongTenant, _testAudience, includeTidClaim: true);
        SetupValidAzureAdMocks();

        // Act
        Func<Task> act = async () => await _validator.ValidateTokenAsync(
            token,
            _testTenantId, // Expect different tenant
            _testAudience,
            validateLifetime: false);

        // Assert
        var exception = await act.Should().ThrowAsync<SecurityTokenValidationException>();
        exception.Which.InnerException.Should().BeOfType<SecurityTokenInvalidIssuerException>();
    }

    // Helper methods

    private string CreateValidToken(
        string tenantId,
        string audience,
        string? userId = null,
        string? email = null,
        string[]? roles = null,
        string? issuer = null,
        bool includeTidClaim = false)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            new RsaSecurityKey(_rsa) { KeyId = _testKeyId },
            SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new Claim("sub", userId ?? "test-user"),
            new Claim("aud", audience)
        };

        if (email != null)
            claims.Add(new Claim("email", email));

        if (roles != null)
        {
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (includeTidClaim)
            claims.Add(new Claim("tid", tenantId));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer ?? $"https://login.microsoftonline.com/{tenantId}/v2.0",
            Audience = audience,
            SigningCredentials = credentials
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private string CreateExpiredToken(string tenantId, string audience)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            new RsaSecurityKey(_rsa) { KeyId = _testKeyId },
            SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", "test-user") }),
            NotBefore = now.AddHours(-2),
            Expires = now.AddHours(-1), // Expired 1 hour ago
            Issuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",
            Audience = audience,
            SigningCredentials = credentials
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private string CreateTokenExpiringIn(TimeSpan timeSpan)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            new RsaSecurityKey(_rsa) { KeyId = _testKeyId },
            SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", "test-user") }),
            Expires = DateTime.UtcNow.Add(timeSpan),
            Issuer = $"https://login.microsoftonline.com/{_testTenantId}/v2.0",
            Audience = _testAudience,
            SigningCredentials = credentials
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private string CreateTokenWithDifferentKey(RSA differentRsa, string tenantId, string audience)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            new RsaSecurityKey(differentRsa) { KeyId = "different-key" },
            SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", "test-user") }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",
            Audience = audience,
            SigningCredentials = credentials
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private string CreateTokenWithoutKeyId(string tenantId, string audience)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            new RsaSecurityKey(_rsa), // No KeyId
            SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", "test-user") }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",
            Audience = audience,
            SigningCredentials = credentials
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private void SetupValidAzureAdMocks()
    {
        SetupOpenIdConfigMock(_testTenantId, $"https://login.microsoftonline.com/{_testTenantId}/v2.0");
        SetupJwksMock();
    }

    private void SetupAzureAdMocksWithV1Endpoint()
    {
        SetupOpenIdConfigMock(_testTenantId, $"https://sts.windows.net/{_testTenantId}/", isV1: true);
        SetupJwksMock();
    }

    private void SetupAzureAdMocksForTenant(string tenantId)
    {
        SetupOpenIdConfigMock(tenantId, $"https://login.microsoftonline.com/{tenantId}/v2.0");
        SetupJwksMock();
    }

    private void SetupOpenIdConfigMock(string tenantId, string issuer, bool isV1 = false)
    {
        var config = new PrimusOpenIdConfiguration
        {
            Issuer = issuer,
            JwksUri = $"https://login.microsoftonline.com/{tenantId}/discovery/v2.0/keys",
            AuthorizationEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize",
            TokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("openid-configuration")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json")
            });
    }

    private void SetupJwksMock()
    {
        var parameters = _rsa.ExportParameters(false);
        var jwk = new PrimusJsonWebKey
        {
            KeyType = "RSA",
            Use = "sig",
            KeyId = _testKeyId,
            Modulus = Base64UrlEncode(parameters.Modulus!),
            Exponent = Base64UrlEncode(parameters.Exponent!)
        };

        var keySet = new PrimusJsonWebKeySet
        {
            Keys = new List<PrimusJsonWebKey> { jwk }
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/keys")),
                ItExpr.IsAny<CancellationToken>())
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

    private void SetupMockToThrowOperationCanceled()
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
    }

    private void SetupMockWithCallCounter()
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                _callCount++;
                
                if (req.RequestUri!.AbsolutePath.Contains("openid-configuration"))
                {
                    var config = new PrimusOpenIdConfiguration
                    {
                        Issuer = $"https://login.microsoftonline.com/{_testTenantId}/v2.0",
                        JwksUri = $"https://login.microsoftonline.com/{_testTenantId}/discovery/v2.0/keys"
                    };
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    var parameters = _rsa.ExportParameters(false);
                    var jwk = new PrimusJsonWebKey
                    {
                        KeyType = "RSA",
                        Use = "sig",
                        KeyId = _testKeyId,
                        Modulus = Base64UrlEncode(parameters.Modulus!),
                        Exponent = Base64UrlEncode(parameters.Exponent!)
                    };
                    var keySet = new PrimusJsonWebKeySet { Keys = new List<PrimusJsonWebKey> { jwk } };
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(keySet), Encoding.UTF8, "application/json")
                    };
                }
            });
    }

    private string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public void Dispose()
    {
        _rsa?.Dispose();
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}

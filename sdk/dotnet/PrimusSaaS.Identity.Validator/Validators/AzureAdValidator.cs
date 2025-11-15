using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Identity.Validator.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator.Validators;

/// <summary>
/// Validates Azure AD JWT tokens using JWKS.
/// </summary>
public class AzureAdValidator
{
    private readonly OpenIdConfigurationService _configService;
    private readonly JwksService _jwksService;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAdValidator"/> class.
    /// </summary>
    /// <param name="configService">OpenID configuration service.</param>
    /// <param name="jwksService">JWKS service.</param>
    public AzureAdValidator(
        OpenIdConfigurationService? configService = null,
        JwksService? jwksService = null)
    {
        _configService = configService ?? new OpenIdConfigurationService();
        _jwksService = jwksService ?? new JwksService();
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Validates an Azure AD JWT token.
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <param name="tenantId">The expected Azure AD tenant ID.</param>
    /// <param name="audience">The expected audience (client ID).</param>
    /// <param name="validateLifetime">Whether to validate token lifetime.</param>
    /// <param name="clockSkew">Clock skew for time validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validated claims principal.</returns>
    public async Task<ClaimsPrincipal> ValidateTokenAsync(
        string token,
        string tenantId,
        string audience,
        bool validateLifetime = true,
        TimeSpan? clockSkew = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new ArgumentException("Audience cannot be null or empty.", nameof(audience));
        }

        // Get OpenID configuration
        var configuration = await _configService.GetConfigurationAsync(tenantId, cancellationToken);

        // Get JWKS
        var jwks = await _jwksService.GetJwksAsync(configuration.JwksUri, cancellationToken);
        var securityKeys = JwksCache.GetSecurityKeys(jwks);

        if (securityKeys.Count == 0)
        {
            throw new InvalidOperationException("No valid signing keys found in JWKS.");
        }

        // Configure validation parameters
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = GetValidIssuers(tenantId),
            
            ValidateAudience = true,
            ValidAudience = audience,
            
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = securityKeys,
            
            ValidateLifetime = validateLifetime,
            ClockSkew = clockSkew ?? TimeSpan.FromMinutes(5),
            
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            
            // Enforce RS256
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }
        };

        try
        {
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Additional validation: ensure tenant ID matches
            var tokenTenantId = ExtractTenantIdFromToken(validatedToken);
            if (!string.IsNullOrEmpty(tokenTenantId) && !tokenTenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityTokenValidationException(
                    $"Token tenant ID '{tokenTenantId}' does not match expected tenant ID '{tenantId}'.");
            }

            return principal;
        }
        catch (SecurityTokenException ex)
        {
            throw new SecurityTokenValidationException("Token validation failed.", ex);
        }
    }

    /// <summary>
    /// Gets valid issuers for the tenant (supports both v1 and v2 endpoints).
    /// </summary>
    private static string[] GetValidIssuers(string tenantId)
    {
        return new[]
        {
            $"https://login.microsoftonline.com/{tenantId}/v2.0",
            $"https://login.microsoftonline.com/{tenantId}/",
            $"https://sts.windows.net/{tenantId}/"
        };
    }

    /// <summary>
    /// Extracts the tenant ID from the token's issuer claim.
    /// </summary>
    private static string? ExtractTenantIdFromToken(SecurityToken token)
    {
        if (token is not JwtSecurityToken jwtToken)
        {
            return null;
        }

        // Try to get tenant ID from 'tid' claim
        var tidClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tid");
        if (tidClaim != null)
        {
            return tidClaim.Value;
        }

        // Try to extract from issuer
        var issuer = jwtToken.Issuer;
        if (string.IsNullOrEmpty(issuer))
        {
            return null;
        }

        // Extract tenant ID from issuer URL
        // Format: https://login.microsoftonline.com/{tenant}/v2.0
        // or: https://sts.windows.net/{tenant}/
        var parts = issuer.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
            var tenantPart = parts[2];
            if (Guid.TryParse(tenantPart, out _))
            {
                return tenantPart;
            }
        }

        return null;
    }

    /// <summary>
    /// Validates a token and returns validation result without throwing exceptions.
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <param name="tenantId">The expected Azure AD tenant ID.</param>
    /// <param name="audience">The expected audience (client ID).</param>
    /// <param name="validateLifetime">Whether to validate token lifetime.</param>
    /// <param name="clockSkew">Clock skew for time validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with success status and principal or error.</returns>
    public async Task<ValidationResult> TryValidateTokenAsync(
        string token,
        string tenantId,
        string audience,
        bool validateLifetime = true,
        TimeSpan? clockSkew = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var principal = await ValidateTokenAsync(
                token, tenantId, audience, validateLifetime, clockSkew, cancellationToken);
            
            return ValidationResult.Success(principal);
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Represents a token validation result.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; init; }
        public ClaimsPrincipal? Principal { get; init; }
        public string? ErrorMessage { get; init; }

        public static ValidationResult Success(ClaimsPrincipal principal) =>
            new() { IsValid = true, Principal = principal };

        public static ValidationResult Failure(string errorMessage) =>
            new() { IsValid = false, ErrorMessage = errorMessage };
    }
}

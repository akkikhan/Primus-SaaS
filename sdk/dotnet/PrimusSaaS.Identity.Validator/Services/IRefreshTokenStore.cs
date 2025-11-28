namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Abstraction for storing and retrieving refresh tokens (durable implementation recommended for production).
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>
    /// Persists a refresh token for a user until the specified expiry.
    /// </summary>
    Task StoreAsync(string refreshToken, string userId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a stored refresh token record; returns null if not found.
    /// </summary>
    Task<RefreshTokenRecord?> GetAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a refresh token as revoked (e.g., after rotation or logout).
    /// </summary>
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Refresh token record returned by stores.
/// </summary>
public class RefreshTokenRecord
{
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Revoked { get; set; }
}

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Result of a refresh-token exchange.
/// </summary>
public class TokenRefreshResult
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? NewRefreshToken { get; set; }
    public string? Error { get; set; }

    public static TokenRefreshResult Failed(string reason) => new() { Success = false, Error = reason };
}

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrimusSaaS.Identity.Validator.Diagnostics;

/// <summary>
/// Categorizes authentication failure reasons for diagnostics.
/// </summary>
public enum AuthFailureReason
{
    /// <summary>Unknown or unclassified failure.</summary>
    Unknown = 0,

    /// <summary>No Authorization header present.</summary>
    MissingToken,

    /// <summary>Authorization header present but malformed.</summary>
    MalformedToken,

    /// <summary>Token issuer (iss) doesn't match any configured issuer.</summary>
    IssuerNotConfigured,

    /// <summary>Token audience (aud) doesn't match configured audiences.</summary>
    AudienceMismatch,

    /// <summary>Token signature verification failed.</summary>
    SignatureInvalid,

    /// <summary>Token has expired (exp claim).</summary>
    TokenExpired,

    /// <summary>Token is not yet valid (nbf claim).</summary>
    TokenNotYetValid,

    /// <summary>Required claim missing from token.</summary>
    MissingRequiredClaim,

    /// <summary>Email verification required but email_verified is false.</summary>
    EmailNotVerified,

    /// <summary>Organization validation failed.</summary>
    OrganizationMismatch,

    /// <summary>Machine-to-machine token rejected (M2M not allowed).</summary>
    MachineToMachineNotAllowed,

    /// <summary>Rate limit exceeded for failed validations.</summary>
    RateLimited,

    /// <summary>JWKS/signing key retrieval failed.</summary>
    KeyRetrievalFailed
}

/// <summary>
/// Represents a single authentication failure event for diagnostics.
/// </summary>
/// <remarks>
/// This record captures non-sensitive metadata about authentication failures
/// for debugging purposes. Token values and sensitive claims are never stored.
/// </remarks>
public record AuthFailureEvent
{
    /// <summary>
    /// UTC timestamp when the failure occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Categorized reason for the failure.
    /// </summary>
    public AuthFailureReason Reason { get; init; }

    /// <summary>
    /// Human-readable description of the failure.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The token's issuer claim (if parseable), or null if token couldn't be parsed.
    /// </summary>
    public string? TokenIssuer { get; init; }

    /// <summary>
    /// The token's audience claim(s) (if parseable), or empty if token couldn't be parsed.
    /// </summary>
    public List<string> TokenAudiences { get; init; } = new();

    /// <summary>
    /// Request path that triggered the failure.
    /// </summary>
    public string? RequestPath { get; init; }

    /// <summary>
    /// Request method (GET, POST, etc.).
    /// </summary>
    public string? RequestMethod { get; init; }

    /// <summary>
    /// Correlation ID from request headers, if present.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// The configured issuer names (not full configuration) for debugging mismatch issues.
    /// </summary>
    public List<string> ConfiguredIssuerNames { get; init; } = new();
}

/// <summary>
/// Thread-safe service for tracking recent authentication failures.
/// </summary>
/// <remarks>
/// <para>
/// This service maintains a circular buffer of recent authentication failures
/// for diagnostic purposes. It's designed for development-time debugging and
/// should have its capacity reduced or disabled in production.
/// </para>
/// <para>
/// Use <see cref="PrimusDiagnosticsOptions.MaxRecentFailures"/> to control buffer size.
/// </para>
/// </remarks>
public class AuthFailureTracker
{
    private readonly ConcurrentQueue<AuthFailureEvent> _failures = new();
    private readonly int _maxCapacity;
    private int _totalFailures;

    /// <summary>
    /// Creates a new tracker with the specified maximum capacity.
    /// </summary>
    /// <param name="maxCapacity">Maximum number of failures to retain. Set to 0 to disable tracking.</param>
    public AuthFailureTracker(int maxCapacity = 50)
    {
        _maxCapacity = Math.Max(0, maxCapacity);
    }

    /// <summary>
    /// Records an authentication failure.
    /// </summary>
    /// <param name="failure">The failure event to record.</param>
    public void RecordFailure(AuthFailureEvent failure)
    {
        if (_maxCapacity == 0) return;

        Interlocked.Increment(ref _totalFailures);
        _failures.Enqueue(failure);

        // Trim to capacity
        while (_failures.Count > _maxCapacity && _failures.TryDequeue(out _))
        {
            // Discard oldest entries
        }
    }

    /// <summary>
    /// Gets the recent failures (up to capacity).
    /// </summary>
    /// <returns>A snapshot of recent failure events, newest first.</returns>
    public IReadOnlyList<AuthFailureEvent> GetRecentFailures()
    {
        return _failures.Reverse().ToList();
    }

    /// <summary>
    /// Gets the total number of failures recorded since startup.
    /// </summary>
    public int TotalFailures => _totalFailures;

    /// <summary>
    /// Gets failures filtered by reason.
    /// </summary>
    /// <param name="reason">The failure reason to filter by.</param>
    /// <returns>Matching failure events.</returns>
    public IReadOnlyList<AuthFailureEvent> GetFailuresByReason(AuthFailureReason reason)
    {
        return _failures.Where(f => f.Reason == reason).Reverse().ToList();
    }

    /// <summary>
    /// Gets failure statistics grouped by reason.
    /// </summary>
    /// <returns>Dictionary of reason to count.</returns>
    public IDictionary<AuthFailureReason, int> GetFailureStats()
    {
        return _failures
            .GroupBy(f => f.Reason)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Clears all recorded failures.
    /// </summary>
    public void Clear()
    {
        while (_failures.TryDequeue(out _)) { }
    }
}

/// <summary>
/// Helper for classifying exceptions into <see cref="AuthFailureReason"/> categories.
/// </summary>
public static class AuthFailureClassifier
{
    /// <summary>
    /// Classifies an exception into an appropriate failure reason.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns>The categorized failure reason.</returns>
    public static AuthFailureReason Classify(Exception? exception)
    {
        if (exception == null) return AuthFailureReason.Unknown;

        var message = exception.Message.ToLowerInvariant();
        var typeName = exception.GetType().Name;

        // Check for common JWT validation exceptions
        if (typeName.Contains("SecurityTokenExpiredException") || message.Contains("lifetime") || message.Contains("expired"))
            return AuthFailureReason.TokenExpired;

        if (typeName.Contains("SecurityTokenNotYetValidException") || message.Contains("not yet valid"))
            return AuthFailureReason.TokenNotYetValid;

        if (typeName.Contains("SecurityTokenInvalidSignatureException") || message.Contains("signature"))
            return AuthFailureReason.SignatureInvalid;

        if (typeName.Contains("SecurityTokenInvalidIssuerException") || message.Contains("issuer"))
            return AuthFailureReason.IssuerNotConfigured;

        if (typeName.Contains("SecurityTokenInvalidAudienceException") || message.Contains("audience"))
            return AuthFailureReason.AudienceMismatch;

        if (message.Contains("email verification") || message.Contains("email_verified"))
            return AuthFailureReason.EmailNotVerified;

        if (message.Contains("organization"))
            return AuthFailureReason.OrganizationMismatch;

        if (message.Contains("machine-to-machine") || message.Contains("m2m") || message.Contains("client_credentials"))
            return AuthFailureReason.MachineToMachineNotAllowed;

        if (message.Contains("jwks") || message.Contains("signing key") || message.Contains("key retrieval"))
            return AuthFailureReason.KeyRetrievalFailed;

        if (typeName.Contains("SecurityToken") && message.Contains("malformed"))
            return AuthFailureReason.MalformedToken;

        return AuthFailureReason.Unknown;
    }

    /// <summary>
    /// Gets a user-friendly description for a failure reason.
    /// </summary>
    /// <param name="reason">The failure reason.</param>
    /// <returns>A human-readable description.</returns>
    public static string GetDescription(AuthFailureReason reason) => reason switch
    {
        AuthFailureReason.MissingToken => "No authorization token was provided in the request.",
        AuthFailureReason.MalformedToken => "The authorization token format is invalid or corrupted.",
        AuthFailureReason.IssuerNotConfigured => "The token's issuer (iss) doesn't match any configured identity provider.",
        AuthFailureReason.AudienceMismatch => "The token's audience (aud) doesn't match any configured audience.",
        AuthFailureReason.SignatureInvalid => "The token signature verification failed. The token may be tampered or the signing key is incorrect.",
        AuthFailureReason.TokenExpired => "The token has expired (exp claim). Request a new token.",
        AuthFailureReason.TokenNotYetValid => "The token is not yet valid (nbf claim). Check for clock synchronization issues.",
        AuthFailureReason.MissingRequiredClaim => "A required claim is missing from the token.",
        AuthFailureReason.EmailNotVerified => "Email verification is required but the email_verified claim is false or missing.",
        AuthFailureReason.OrganizationMismatch => "The token's organization claim doesn't match the required organization.",
        AuthFailureReason.MachineToMachineNotAllowed => "Machine-to-machine (M2M/client_credentials) tokens are not allowed for this endpoint.",
        AuthFailureReason.RateLimited => "Too many authentication failures. Please wait before retrying.",
        AuthFailureReason.KeyRetrievalFailed => "Failed to retrieve signing keys from the JWKS endpoint.",
        _ => "An unknown authentication error occurred."
    };
}

/// <summary>
/// JSON serialization context for diagnostics types.
/// </summary>
[JsonSerializable(typeof(AuthFailureEvent))]
[JsonSerializable(typeof(List<AuthFailureEvent>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class DiagnosticsJsonContext : JsonSerializerContext
{
}

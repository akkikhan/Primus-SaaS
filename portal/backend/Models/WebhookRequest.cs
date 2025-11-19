namespace PrimusSaaS.Portal.Api.Models;

/// <summary>
/// Audit trail entity for all webhook requests received by the system.
/// Tracks both successful and failed webhook attempts for security and debugging.
/// </summary>
public class WebhookRequest
{
    /// <summary>
    /// Unique identifier for the webhook request
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The webhook endpoint that received the request (e.g., /api/webhooks/npm-registry)
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Registry type: npm, nuget, etc.
    /// </summary>
    public string RegistryType { get; set; } = string.Empty;

    /// <summary>
    /// Raw JSON payload received in the request body
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// HMAC signature from the request header (X-Npm-Signature or X-NuGet-Signature)
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the client that sent the webhook request
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code returned (200, 401, 404, 500, etc.)
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response body sent back to the client (success message or error details)
    /// </summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the webhook request was received
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Processing duration in milliseconds
    /// </summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>
    /// Whether signature validation passed
    /// </summary>
    public bool SignatureValid { get; set; }

    /// <summary>
    /// Event type from payload (e.g., package:publish, PackagePushed)
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Package name/ID from the payload
    /// </summary>
    public string? PackageName { get; set; }

    /// <summary>
    /// Package version from the payload
    /// </summary>
    public string? PackageVersion { get; set; }
}

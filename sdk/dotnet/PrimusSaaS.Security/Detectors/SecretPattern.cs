using System.Text.Json.Serialization;

namespace PrimusSaaS.Security.Detectors;

/// <summary>
/// Represents a pattern for detecting secrets.
/// </summary>
public class SecretPattern
{
    /// <summary>
    /// Gets or sets the unique identifier for the pattern.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of secret (e.g., "AWS Access Key").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name (e.g., "Amazon Web Services").
    /// </summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the regex pattern to match.
    /// </summary>
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum entropy threshold (optional).
    /// </summary>
    [JsonPropertyName("entropy_threshold")]
    public double? EntropyThreshold { get; set; }

    /// <summary>
    /// Gets or sets the severity level (e.g., "CRITICAL").
    /// </summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "MEDIUM";

    /// <summary>
    /// Gets or sets the confidence level (e.g., "HIGH").
    /// </summary>
    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = "LOW";

    /// <summary>
    /// Gets or sets the description of the secret.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remediation steps.
    /// </summary>
    [JsonPropertyName("remediation")]
    public string Remediation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CWE identifier.
    /// </summary>
    [JsonPropertyName("cwe")]
    public string Cwe { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OWASP category.
    /// </summary>
    [JsonPropertyName("owasp")]
    public string Owasp { get; set; } = string.Empty;
}



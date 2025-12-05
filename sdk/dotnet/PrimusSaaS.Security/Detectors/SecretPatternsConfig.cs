using System.Text.Json.Serialization;

namespace PrimusSaaS.Security.Detectors;

/// <summary>
/// Configuration container for secret patterns.
/// </summary>
public class SecretPatternsConfig
{
    /// <summary>
    /// Gets or sets the list of secret patterns.
    /// </summary>
    [JsonPropertyName("patterns")]
    public List<SecretPattern> Patterns { get; set; } = new();
}

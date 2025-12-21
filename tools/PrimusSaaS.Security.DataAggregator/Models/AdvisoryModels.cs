
using System.Text.Json.Serialization;

namespace PrimusSaaS.Security.DataAggregator.Models;

public class SecurityAdvisory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty; // NOTE: Some are array, usually string in GHSA export

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; set; } = new();

    [JsonPropertyName("modified")]
    public DateTime? Modified { get; set; }

    [JsonPropertyName("published")]
    public DateTime? Published { get; set; }

    [JsonPropertyName("affected")]
    public List<AffectedPackage> Affected { get; set; } = new();

    [JsonPropertyName("references")]
    public List<Reference> References { get; set; } = new();
}

public class AffectedPackage
{
    [JsonPropertyName("package")]
    public PackageInfo Package { get; set; } = new();

    [JsonPropertyName("ranges")]
    public List<VersionRange> Ranges { get; set; } = new();

    [JsonPropertyName("versions")]
    public List<string> Versions { get; set; } = new();
}

public class PackageInfo
{
    [JsonPropertyName("ecosystem")]
    public string Ecosystem { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class VersionRange
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<RangeEvent> Events { get; set; } = new();
}

public class RangeEvent
{
    [JsonPropertyName("introduced")]
    public string Introduced { get; set; }

    [JsonPropertyName("fixed")]
    public string Fixed { get; set; }

    [JsonPropertyName("last_affected")]
    public string LastAffected { get; set; }
}

public class Reference
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

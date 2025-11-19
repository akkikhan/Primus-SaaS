namespace PrimusSaaS.Portal.Api.Models;

/// <summary>
/// Represents the webhook payload from NuGet.org when a package is published
/// </summary>
public class NuGetWebhookPayload
{
    public string Event { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string PackageType { get; set; } = string.Empty;
    public DateTime Published { get; set; }
    public NuGetPackageMetadata? Metadata { get; set; }
}

/// <summary>
/// Additional metadata about the NuGet package
/// </summary>
public class NuGetPackageMetadata
{
    public string? IconUrl { get; set; }
    public string? ProjectUrl { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? LicenseUrl { get; set; }
    public string? Description { get; set; }
    public string[]? Authors { get; set; }
    public string[]? Tags { get; set; }
    public NuGetPackageUrls? Urls { get; set; }
}

/// <summary>
/// URLs for accessing the NuGet package
/// </summary>
public class NuGetPackageUrls
{
    public string? PackageDetails { get; set; }
    public string? PackageDownload { get; set; }
}

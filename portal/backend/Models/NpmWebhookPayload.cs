namespace PrimusSaaS.Portal.Api.Models;

/// <summary>
/// Represents the payload structure from npm registry webhooks
/// </summary>
public class NpmWebhookPayload
{
    public string Event { get; set; } = string.Empty; // "package:publish", "package:unpublish", etc.
    public string Name { get; set; } = string.Empty; // Package name (e.g., "primus-identity-validator")
    public string Type { get; set; } = string.Empty; // "package"
    public string Version { get; set; } = string.Empty; // Version published (e.g., "1.0.1")
    public NpmPackageData? Change { get; set; }
    public long Time { get; set; } // Unix timestamp
    public string HookOwner { get; set; } = string.Empty;
}

public class NpmPackageData
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NpmDistInfo? Dist { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class NpmDistInfo
{
    public string Tarball { get; set; } = string.Empty;
    public string Shasum { get; set; } = string.Empty;
    public string Integrity { get; set; } = string.Empty;
}

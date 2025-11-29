using System.ComponentModel.DataAnnotations;

namespace PrimusSaaS.Identity.Validator.Services;

public static class ApiKeyDefaults
{
    public const string Scheme = "PrimusApiKey";
    public const string DefaultHeaderName = "X-API-Key";
}

/// <summary>
/// API key authentication configuration.
/// </summary>
public class ApiKeyOptions
{
    /// <summary>
    /// Enable API key authentication alongside JWT Bearer.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Header name to read the API key from. Default: X-API-Key.
    /// </summary>
    public string HeaderName { get; set; } = ApiKeyDefaults.DefaultHeaderName;

    /// <summary>
    /// Static API key entries.
    /// </summary>
    public List<ApiKeyEntry> Keys { get; set; } = new();

    internal void Validate()
    {
        if (!Enabled) return;

        if (string.IsNullOrWhiteSpace(HeaderName))
        {
            throw new ValidationException("ApiKeyOptions.HeaderName is required when API key auth is enabled.");
        }

        if (Keys == null || Keys.Count == 0)
        {
            throw new ValidationException("At least one API key entry is required when API key auth is enabled.");
        }

        if (Keys.Any(k => string.IsNullOrWhiteSpace(k.Key)))
        {
            throw new ValidationException("API key entries must have non-empty Key values.");
        }
    }
}

public class ApiKeyEntry
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = "api-key";
    public string? TenantId { get; set; }
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, string> Claims { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

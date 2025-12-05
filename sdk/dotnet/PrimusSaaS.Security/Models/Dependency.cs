namespace PrimusSaaS.Security.Models;

/// <summary>
/// Represents a project dependency.
/// </summary>
public class Dependency
{
    /// <summary>
    /// Gets or sets the package name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ecosystem (e.g., "nuget", "npm").
    /// </summary>
    public string Ecosystem { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path where the dependency is defined.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}

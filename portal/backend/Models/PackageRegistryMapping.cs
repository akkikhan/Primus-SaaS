namespace PrimusSaaS.Portal.Api.Models;

/// <summary>
/// Maps external package registry names (npm/NuGet) to internal Module IDs
/// </summary>
public class PackageRegistryMapping
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    
    /// <summary>
    /// Registry type: "npm", "nuget"
    /// </summary>
    public string RegistryType { get; set; } = string.Empty;
    
    /// <summary>
    /// Package name in the registry (e.g., "primus-identity-validator")
    /// </summary>
    public string PackageName { get; set; } = string.Empty;
    
    /// <summary>
    /// When this mapping was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Module Module { get; set; } = null!;
}

namespace PrimusSaaS.Portal.Api.Models;

public class ApplicationModule
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public int ModuleId { get; set; }
    public int ModuleVersionId { get; set; }
    public string ConfigJson { get; set; } = string.Empty; // JSON config for module integration
    public DateTime IntegratedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Application Application { get; set; } = null!;
    public Module Module { get; set; } = null!;
    public ModuleVersion ModuleVersion { get; set; } = null!;
}

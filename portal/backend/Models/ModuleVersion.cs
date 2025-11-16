namespace PrimusSaaS.Portal.Api.Models;

public class ModuleVersion
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string Version { get; set; } = string.Empty;
    public bool IsBreakingChange { get; set; }
    public string ReleaseNotes { get; set; } = string.Empty;
    public string Changelog { get; set; } = string.Empty;
    public string DemoCode { get; set; } = string.Empty;
    public string SupportedStacksJson { get; set; } = string.Empty; // JSON array of supported stacks
    public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Module Module { get; set; } = null!;
    public ICollection<ApplicationModule> ApplicationModules { get; set; } = new List<ApplicationModule>();
}

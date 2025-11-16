namespace PrimusSaaS.Portal.Api.Models;

public class Module
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<ModuleVersion> Versions { get; set; } = new List<ModuleVersion>();
    public ICollection<ApplicationModule> ApplicationModules { get; set; } = new List<ApplicationModule>();
}

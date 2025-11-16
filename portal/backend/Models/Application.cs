namespace PrimusSaaS.Portal.Api.Models;

public class Application
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AppStack Stack { get; set; }
    public string PrimusClientId { get; set; } = string.Empty; // Auto-generated tracking ID (e.g., PSP-CLI-000123)
    public string? Description { get; set; } // Optional description
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<ApplicationModule> ApplicationModules { get; set; } = new List<ApplicationModule>();
}

public enum AppStack
{
    DotNet = 1,
    NodeJS = 2,
    Python = 3,
    NodeJSNest = 4,
    TypeScriptLib = 5
}

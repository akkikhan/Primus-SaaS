namespace PrimusSaaS.Portal.Api.Models;

public class Application
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AppStack Stack { get; set; }
    public string PrimusClientId { get; set; } = string.Empty; // Legacy field for SDK usage
    public string ClientId { get; set; } = string.Empty; // Client ID for authentication
    public string ClientSecret { get; set; } = string.Empty; // Client Secret for authentication
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
    Python = 3
}

using System.ComponentModel.DataAnnotations;

namespace PrimusSaaS.Portal.Api.Models;

public class NotificationPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool EmailOnNewVersion { get; set; } = true;
    public bool EmailOnBreakingChange { get; set; } = true;
    public bool EmailOnSecurityUpdate { get; set; } = true;
    public string? AdditionalEmails { get; set; } // Comma separated list of emails
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

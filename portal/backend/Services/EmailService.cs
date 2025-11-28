using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using PrimusSaaS.Portal.Api.Services;

using PrimusSaaS.Portal.Api.Services;
using Primus.Notifications.Core;
using PrimusSaaS.Portal.Api.Notifications;
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly PortalDbContext _context;
    private readonly string _docsBaseUrl;
    private readonly NotificationService _notificationService;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger, PortalDbContext context, NotificationService notificationService)
    {
        _settings = options.Value;
        _logger = logger;
        _context = context;
        _notificationService = notificationService;
        _docsBaseUrl = (_settings.DocsBaseUrl ?? "https://akkikhan.github.io/Primus-SaaS").TrimEnd('/');
    }



    public async Task SendApplicationCreatedAsync(Application app, string clientSecret, string? recipientEmail = null)
    {
        var to = recipientEmail ?? app.Owner.Email;
        if (string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("No email address found for application creation notification");
            return;
        }

        var notification = new ApplicationCreatedNotification(app.Name, app.PrimusClientId, to);
        await _notificationService.SendAsync(notification);
    }

    public async Task SendModuleAssignedAsync(Application app, ModuleVersion version)
    {
        var to = await ResolveOwnerEmailAsync(app);
        if (string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("No email address found for module assignment notification for application {AppId}", app.Id);
            return;
        }

        var moduleName = version.Module.Name;
        var stack = app.Stack.ToString().ToLowerInvariant();
        // Get package name and install command based on module and stack
        var(packageName, installCommand) = GetPackageInfo(moduleName, stack);
        // Generate stack-specific documentation link
        var docLink = GenerateDocLink(moduleName, stack);
        
        var notification = new ModuleAssignedNotification(
            to, 
            moduleName, 
            version.Version, 
            app.Name, 
            installCommand, 
            docLink, 
            stack, 
            _docsBaseUrl
        );

        await _notificationService.SendAsync(notification);
    }

    private (string packageName, string installCommand) GetPackageInfo(string moduleName, string stack)
    {
        var normalizedStack = stack.ToLowerInvariant();
        return (moduleName.ToLowerInvariant(), normalizedStack) switch
        {
            ("identity validator", "dotnet") => ("PrimusSaaS.Identity.Validator", "dotnet add package PrimusSaaS.Identity.Validator"),
            ("identity validator", "nodejs") or ("identity validator", "nodejs-nest") => ("@primus-saas/identity-validator", "npm install @primus-saas/identity-validator"),
            ("logging", "dotnet") or ("logging sdk", "dotnet") => ("PrimusSaaS.Logging", "dotnet add package PrimusSaaS.Logging"),
            ("logging", "nodejs") or ("logging", "nodejs-nest") or ("logging sdk", "nodejs") or ("logging sdk", "nodejs-nest") => ("@primus-saas/logging", "npm install @primus-saas/logging"),
            _ => ("Unknown", "# Package not found")};
    }

    private string GenerateDocLink(string moduleName, string stack)
    {
        var baseUrl = _docsBaseUrl.TrimEnd('/');
        var anchor = moduleName.ToLowerInvariant().Contains("log") ? "#add-logging" : "#5-integration-steps";
        return $"{baseUrl}/docs/modules/client-integration-guide{anchor}";
    }

    public async Task SendVersionPublishedAsync(Application app, ModuleVersion version)
    {
        if (!await ShouldSendForVersionAsync(version))
        {
            _logger.LogInformation("Skipping version email for module {ModuleId} version {Version} (non-major policy)", version.ModuleId, version.Version);
            return;
        }

        // Check user preferences
        var pref = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == app.OwnerUserId);
        // Default to true if no preference set
        bool shouldSend = true;
        if (pref != null)
        {
            if (version.IsBreakingChange)
            {
                shouldSend = pref.EmailOnBreakingChange;
            }
            else
            {
                shouldSend = pref.EmailOnNewVersion;
            }
        }

        if (!shouldSend)
        {
            _logger.LogInformation("Skipping email for user {UserId} due to preferences", app.OwnerUserId);
            return;
        }

        var to = await ResolveOwnerEmailAsync(app);
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("No owner email found for application {AppId}; skipping version notification", app.Id);
            return;
        }
        
        var npmMapping = await _context.PackageRegistryMappings.FirstOrDefaultAsync(m => m.ModuleId == version.ModuleId && m.RegistryType == "npm");
        var nugetMapping = await _context.PackageRegistryMappings.FirstOrDefaultAsync(m => m.ModuleId == version.ModuleId && m.RegistryType == "nuget");
        var npmPackageName = npmMapping?.PackageName ?? "unknown-package";
        var nugetPackageName = nugetMapping?.PackageName ?? "Unknown.Package";

        var notification = new VersionPublishedNotification(
            to,
            version.Version,
            version.Module.Name,
            npmPackageName,
            nugetPackageName,
            version.ReleaseNotes,
            version.Changelog,
            version.IsBreakingChange
        );

        await _notificationService.SendAsync(notification);

        // Send to additional emails if configured
        if (pref != null && !string.IsNullOrEmpty(pref.AdditionalEmails))
        {
            var emails = pref.AdditionalEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var email in emails)
            {
                var additionalNotification = new VersionPublishedNotification(
                    email,
                    version.Version,
                    version.Module.Name,
                    npmPackageName,
                    nugetPackageName,
                    version.ReleaseNotes,
                    version.Changelog,
                    version.IsBreakingChange
                );
                await _notificationService.SendAsync(additionalNotification);
            }
        }
    }

    private async Task<string?> ResolveOwnerEmailAsync(Application app)
    {
        if (app.Owner != null && !string.IsNullOrWhiteSpace(app.Owner.Email))
        {
            return app.Owner.Email;
        }

        return await _context.Users.Where(u => u.Id == app.OwnerUserId).Select(u => u.Email).FirstOrDefaultAsync();
    }

    private async Task<bool> ShouldSendForVersionAsync(ModuleVersion version)
    {
        // Major-only policy: send if new major is greater than previous major for this module.
        if (!TryParseSemVer(version.Version, out var newSemVer))
        {
            // If parsing fails, err on the side of sending to avoid missing critical updates.
            return true;
        }

        var previous = await _context.ModuleVersions.Where(mv => mv.ModuleId == version.ModuleId && mv.Id != version.Id).OrderByDescending(mv => mv.Id).Select(mv => mv.Version).ToListAsync();
        if (previous.Count == 0)
        {
            return true;
        }

        var latestParsed = previous.Select(v =>
        {
            if (TryParseSemVer(v, out var semVer))
            {
                return ((int Major, int Minor, int Patch)? )semVer;
            }

            return null;
        }).Where(v => v.HasValue).OrderByDescending(v => v!.Value.Major).ThenByDescending(v => v!.Value.Minor).ThenByDescending(v => v!.Value.Patch).FirstOrDefault();
        if (!latestParsed.HasValue)
        {
            return true;
        }

        return newSemVer.Major > latestParsed.Value.Major;
    }

    private static bool TryParseSemVer(string version, out (int Major, int Minor, int Patch) semVer)
    {
        semVer = (0, 0, 0);
        var parts = version.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1 || parts.Length > 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var major))
            return false;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var m) ? m : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;
        semVer = (major, minor, patch);
        return true;
    }
}

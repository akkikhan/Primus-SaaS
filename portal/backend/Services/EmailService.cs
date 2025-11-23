using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using PrimusSaaS.Portal.Api.Services;

namespace PrimusSaaS.Portal.Api.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly PortalDbContext _context;
    private readonly string _docsBaseUrl;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger, PortalDbContext context)
    {
        _settings = options.Value;
        _logger = logger;
        _context = context;
        _docsBaseUrl = (_settings.DocsBaseUrl ?? "http://localhost:3001").TrimEnd('/');
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(to));

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass),
                EnableSsl = _settings.EnableSsl
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    public async Task SendApplicationCreatedAsync(Application app, string clientSecret, string? recipientEmail = null)
    {
        var to = recipientEmail ?? app.Owner.Email;
        if (string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("No email address found for application creation notification");
            return;
        }
        var subject = "Your new Primus application has been created";
        var body = $@"<p>Hello,</p>
<p>Your application <strong>{app.Name}</strong> has been created.</p>
<p>Client ID: <code>{app.PrimusClientId}</code></p>
<p>Modules are not assigned yet. We will send integration steps as soon as your module(s) are added.</p>
<p>Best regards,<br/>Primus SaaS Team</p>";
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendModuleAssignedAsync(Application app, ModuleVersion version)
    {
        var to = await ResolveOwnerEmailAsync(app);
        if (string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("No email address found for module assignment notification for application {AppId}", app.Id);
            return;
        }
        var subject = $"Module {version.Module.Name} assigned to {app.Name}";
        
        var npmMapping = await _context.PackageRegistryMappings
            .FirstOrDefaultAsync(m => m.ModuleId == version.ModuleId && m.RegistryType == "npm");
        var nugetMapping = await _context.PackageRegistryMappings
            .FirstOrDefaultAsync(m => m.ModuleId == version.ModuleId && m.RegistryType == "nuget");

        var npmPackageName = npmMapping?.PackageName ?? "unknown-package";
        var nugetPackageName = nugetMapping?.PackageName ?? "Unknown.Package";
        var docsLink = $"{_docsBaseUrl}{ModuleDocsMapper.GetIntegrationPath(app.Stack)}";

        var body = $@"<p>Hello,</p>
<p>The module <strong>{version.Module.Name}</strong> (v{version.Version}) has been assigned to your application <strong>{app.Name}</strong>.</p>
<p>You can now integrate it using the following commands:</p>
<ul>
<li>npm: <code>npm install {npmPackageName}@{version.Version}</code></li>
<li>NuGet: <code>Install-Package {nugetPackageName} -Version {version.Version}</code></li>
</ul>
<p>Integration guide: <a href=""{docsLink}"">{docsLink}</a></p>
<p>Best regards,<br/>Primus SaaS Team</p>";
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendVersionPublishedAsync(Application app, ModuleVersion version)
    {
        if (!await ShouldSendForVersionAsync(version))
        {
            _logger.LogInformation("Skipping version email for module {ModuleId} version {Version} (non-major policy)", version.ModuleId, version.Version);
        // Check user preferences
        var pref = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == app.OwnerUserId);

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

        var to = app.Owner.Email;
        var subject = $"New version {version.Version} published for module {version.Module.Name}";
        var npmMapping = await _context.PackageRegistryMappings
            .FirstOrDefaultAsync(m => m.ModuleId == version.ModuleId && m.RegistryType == "npm");
        var nugetMapping = await _context.PackageRegistryMappings
            .FirstOrDefaultAsync(m => m.ModuleId == version.ModuleId && m.RegistryType == "nuget");

        var npmPackageName = npmMapping?.PackageName ?? "unknown-package";
        var nugetPackageName = nugetMapping?.PackageName ?? "Unknown.Package";

        var body = $@"<p>Hello,</p>
<p>A new version <strong>{version.Version}</strong> of the module <strong>{version.Module.Name}</strong> has been published.</p>

<h3>📦 Installation</h3>
<ul>
<li><strong>npm:</strong> <code>npm install {npmPackageName}@{version.Version}</code></li>
<li><strong>NuGet:</strong> <code>Install-Package {nugetPackageName} -Version {version.Version}</code></li>
</ul>

<h3>📝 Release Information</h3>
<p><strong>Release notes:</strong> {version.ReleaseNotes}</p>
<p><strong>Changelog:</strong> {version.Changelog}</p>
{(version.IsBreakingChange ? "<p><strong>⚠️ BREAKING CHANGE:</strong> This version contains breaking changes. Please review the changelog carefully before upgrading.</p>" : "")}

<h3>⚠️ Important: Azure AD Configuration</h3>
<p>If you're using Azure AD, remember to configure the <code>audiences</code> array with your <strong>Azure AD Client ID</strong>, not the Primus App ID.</p>
<p><a href=""https://github.com/akkikhan/Primus-SaaS/blob/main/sdk/nodejs/primus-identity-validator/README.md#%EF%B8%8F-critical-azure-ad-audience-configuration"">View Azure AD Configuration Guide →</a></p>

<h3>📚 Resources</h3>
<ul>
<li><a href=""https://github.com/akkikhan/Primus-SaaS/blob/main/sdk/nodejs/primus-identity-validator/README.md"">Full Documentation</a></li>
<li><a href=""http://localhost:5173/applications"">Portal Dashboard</a></li>
<li><a href=""https://github.com/akkikhan/Primus-SaaS/tree/main/test-apps/acme-dashboard"">Example Project</a></li>
</ul>

<p>Need help? Reply to this email or visit our <a href=""https://github.com/akkikhan/Primus-SaaS/issues"">support portal</a>.</p>

<p>Best regards,<br/>Primus SaaS Team</p>";
        await SendEmailAsync(to, subject, body);

        // Send to additional emails if configured
            var emails = pref.AdditionalEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var email in emails)
            {
                await SendEmailAsync(email, subject, body);
            }
        }
    }

    private async Task<string?> ResolveOwnerEmailAsync(Application app)
    {
        if (app.Owner != null && !string.IsNullOrWhiteSpace(app.Owner.Email))
        {
            return app.Owner.Email;
        }

        return await _context.Users
            .Where(u => u.Id == app.OwnerUserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();
    }

    private async Task<bool> ShouldSendForVersionAsync(ModuleVersion version)
    {
        // Major-only policy: send if new major is greater than previous major for this module.
        if (!TryParseSemVer(version.Version, out var newSemVer))
        {
            // If parsing fails, err on the side of sending to avoid missing critical updates.
            return true;
        }

        var previous = await _context.ModuleVersions
            .Where(mv => mv.ModuleId == version.ModuleId && mv.Id != version.Id)
            .OrderByDescending(mv => mv.Id)
            .Select(mv => mv.Version)
            .ToListAsync();

        if (previous.Count == 0)
        {
            return true;
        }

        var latestParsed = previous
            .Select(v => 
            {
                if (TryParseSemVer(v, out var semVer))
                {
                    return ((int Major, int Minor, int Patch)?)semVer;
                }
                return null;
            })
            .Where(v => v.HasValue)
            .OrderByDescending(v => v!.Value.Major)
            .ThenByDescending(v => v!.Value.Minor)
            .ThenByDescending(v => v!.Value.Patch)
            .FirstOrDefault();

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

        if (!int.TryParse(parts[0], out var major)) return false;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var m) ? m : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;
        semVer = (major, minor, patch);
        return true;
    }
}

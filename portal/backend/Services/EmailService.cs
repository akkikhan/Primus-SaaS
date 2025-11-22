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

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger, PortalDbContext context)
    {
        _settings = options.Value;
        _logger = logger;
        _context = context;
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
{GetIntegrationInstructions(app)}
<p>Best regards,<br/>Primus SaaS Team</p>";
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendModuleAssignedAsync(Application app, ModuleVersion version)
    {
        var to = app.Owner.Email;
        var subject = $"Module {version.Module.Name} assigned to {app.Name}";
        
        var npmMapping = await _context.PackageRegistryMappings
            .FirstOrDefaultAsync(m => m.ModuleId == version.ModuleId && m.RegistryType == "npm");
        var nugetMapping = await _context.PackageRegistryMappings
            .FirstOrDefaultAsync(m => m.ModuleId == version.ModuleId && m.RegistryType == "nuget");

        var npmPackageName = npmMapping?.PackageName ?? "unknown-package";
        var nugetPackageName = nugetMapping?.PackageName ?? "Unknown.Package";

        var body = $@"<p>Hello,</p>
<p>The module <strong>{version.Module.Name}</strong> (v{version.Version}) has been assigned to your application <strong>{app.Name}</strong>.</p>
<p>You can now integrate it using the following commands:</p>
<ul>
<li>npm: <code>npm install {npmPackageName}@{version.Version}</code></li>
<li>NuGet: <code>Install-Package {nugetPackageName} -Version {version.Version}</code></li>
</ul>
<p>Best regards,<br/>Primus SaaS Team</p>";
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendVersionPublishedAsync(Application app, ModuleVersion version)
    {
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
<p>Release notes: {version.ReleaseNotes}</p>
<p>Changelog: {version.Changelog}</p>
<p>Installation commands:</p>
<ul>
<li>npm: <code>npm install {npmPackageName}@{version.Version}</code></li>
<li>NuGet: <code>Install-Package {nugetPackageName} -Version {version.Version}</code></li>
</ul>
<p>Best regards,<br/>Primus SaaS Team</p>";
        await SendEmailAsync(to, subject, body);

        // Send to additional emails if configured
        if (pref?.AdditionalEmails != null)
        {
            var emails = pref.AdditionalEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var email in emails)
            {
                await SendEmailAsync(email, subject, body);
            }
        }
    }

    private string GetIntegrationInstructions(Application app)
    {
        var docsUrl = $"http://localhost:3000/docs/{app.Id}";
        var sb = new System.Text.StringBuilder();
        sb.Append($"<p><strong>Integration Guide:</strong></p>");
        sb.Append($"<p>View your full documentation here: <a href='{docsUrl}'>{docsUrl}</a></p>");

        switch (app.Stack)
        {
            case AppStack.NodeJS:
            case AppStack.NodeJSNest:
            case AppStack.TypeScriptLib:
                sb.Append("<p>To get started with Node.js/TypeScript:</p>");
                sb.Append("<pre>npm install @primus/sdk</pre>");
                sb.Append("<p>Initialize the client with your Client ID and Secret.</p>");
                break;
            case AppStack.DotNet:
                sb.Append("<p>To get started with .NET:</p>");
                sb.Append("<pre>dotnet add package Primus.Sdk</pre>");
                sb.Append("<p>Add the Primus service in your Program.cs.</p>");
                break;
            case AppStack.Python:
                sb.Append("<p>To get started with Python:</p>");
                sb.Append("<pre>pip install primus-sdk</pre>");
                break;
            default:
                sb.Append("<p>Please refer to the documentation portal for integration steps.</p>");
                break;
        }

        return sb.ToString();
    }
}

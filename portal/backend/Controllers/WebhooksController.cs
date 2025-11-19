using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using PrimusSaaS.Portal.Api.Services;
using System.Text.Json;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly PortalDbContext _context;
    private readonly IWebhookSignatureValidator _signatureValidator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        PortalDbContext context,
        IWebhookSignatureValidator signatureValidator,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _context = context;
        _signatureValidator = signatureValidator;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("npm-registry")]
    public async Task<IActionResult> HandleNpmWebhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            var signature = Request.Headers["X-Npm-Signature"].FirstOrDefault();
            var secret = _configuration["Webhooks:NpmSecret"];

            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("npm webhook secret not configured");
                return StatusCode(500, new { error = "Webhook configuration error" });
            }

            if (string.IsNullOrEmpty(signature) || !_signatureValidator.ValidateNpmSignature(rawBody, signature, secret))
            {
                _logger.LogWarning("Invalid npm webhook signature");
                return Unauthorized(new { error = "Invalid signature" });
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var payload = JsonSerializer.Deserialize<NpmWebhookPayload>(rawBody, options);

            if (payload == null)
            {
                _logger.LogWarning("Failed to parse npm webhook payload");
                return BadRequest(new { error = "Invalid payload" });
            }

            if (payload.Event != "package:publish")
            {
                _logger.LogInformation("Ignoring npm webhook event: {Event}", payload.Event);
                return Ok(new { message = "Event ignored", eventType = payload.Event });
            }

            _logger.LogInformation("Received npm publish webhook for {Package} v{Version}", payload.Name, payload.Version);

            var mapping = await _context.PackageRegistryMappings
                .Include(m => m.Module)
                .FirstOrDefaultAsync(m => m.RegistryType == "npm" && m.PackageName == payload.Name);

            if (mapping == null)
            {
                _logger.LogWarning("No module mapping found for npm package: {Package}", payload.Name);
                return NotFound(new { error = "Package not registered", package = payload.Name });
            }

            var existingVersion = await _context.ModuleVersions
                .FirstOrDefaultAsync(mv => mv.ModuleId == mapping.ModuleId && mv.Version == payload.Version);

            if (existingVersion != null)
            {
                _logger.LogInformation("Version {Version} already exists for module {ModuleId}", payload.Version, mapping.ModuleId);
                return Ok(new { message = "Version already exists", version = payload.Version });
            }

            var moduleVersion = new ModuleVersion
            {
                ModuleId = mapping.ModuleId,
                Version = payload.Version,
                ReleasedAt = DateTimeOffset.FromUnixTimeSeconds(payload.Time).DateTime,
                ReleaseNotes = $"Published to npm registry",
                IsBreakingChange = IsBreakingChange(payload.Version),
                Changelog = payload.Change?.Dist?.Tarball ?? "",
                DemoCode = "",
                SupportedStacksJson = "[]"
            };

            _context.ModuleVersions.Add(moduleVersion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created module version {Version} for module {ModuleId}", payload.Version, mapping.ModuleId);

            return Ok(new
            {
                message = "Version created successfully",
                moduleId = mapping.ModuleId,
                version = payload.Version
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing npm webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("test")]
    public IActionResult TestWebhook()
    {
        var secret = _configuration["Webhooks:NpmSecret"];
        return Ok(new
        {
            webhookConfigured = !string.IsNullOrEmpty(secret),
            message = string.IsNullOrEmpty(secret) ? "Webhook secret not configured" : "Webhook is configured"
        });
    }

    private bool IsBreakingChange(string version)
    {
        var parts = version.Split('.');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int major))
        {
            return major > 0;
        }
        return false;
    }
}

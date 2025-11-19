using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using PrimusSaaS.Portal.Api.Services;
using System.Diagnostics;
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
        var stopwatch = Stopwatch.StartNew();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();
        
        var signature = Request.Headers["X-Npm-Signature"].FirstOrDefault() ?? "";
        
        var webhookRequest = new WebhookRequest
        {
            Endpoint = "/api/webhooks/npm-registry",
            RegistryType = "npm",
            Payload = rawBody,
            Signature = signature,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var secret = _configuration["Webhooks:NpmSecret"];

            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("npm webhook secret not configured");
                var errorResponse = new { error = "Webhook configuration error" };
                webhookRequest.StatusCode = 500;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(errorResponse);
                webhookRequest.SignatureValid = false;
                return StatusCode(500, errorResponse);
            }

            webhookRequest.SignatureValid = !string.IsNullOrEmpty(signature) && _signatureValidator.ValidateNpmSignature(rawBody, signature, secret);

            if (!webhookRequest.SignatureValid)
            {
                _logger.LogWarning("Invalid npm webhook signature");
                var errorResponse = new { error = "Invalid signature" };
                webhookRequest.StatusCode = 401;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(errorResponse);
                return Unauthorized(errorResponse);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var payload = JsonSerializer.Deserialize<NpmWebhookPayload>(rawBody, options);

            if (payload == null)
            {
                _logger.LogWarning("Failed to parse npm webhook payload");
                var errorResponse = new { error = "Invalid payload" };
                webhookRequest.StatusCode = 400;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(errorResponse);
                return BadRequest(errorResponse);
            }

            webhookRequest.EventType = payload.Event;
            webhookRequest.PackageName = payload.Name;
            webhookRequest.PackageVersion = payload.Version;

            if (payload.Event != "package:publish")
            {
                _logger.LogInformation("Ignoring npm webhook event: {Event}", payload.Event);
                var successResponse = new { message = "Event ignored", eventType = payload.Event };
                webhookRequest.StatusCode = 200;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(successResponse);
                return Ok(successResponse);
            }

            _logger.LogInformation("Received npm publish webhook for {Package} v{Version}", payload.Name, payload.Version);

            var mapping = await _context.PackageRegistryMappings
                .Include(m => m.Module)
                .FirstOrDefaultAsync(m => m.RegistryType == "npm" && m.PackageName == payload.Name);

            if (mapping == null)
            {
                _logger.LogWarning("No module mapping found for npm package: {Package}", payload.Name);
                var errorResponse = new { error = "Package not registered", package = payload.Name };
                webhookRequest.StatusCode = 404;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(errorResponse);
                return NotFound(errorResponse);
            }

            var existingVersion = await _context.ModuleVersions
                .FirstOrDefaultAsync(mv => mv.ModuleId == mapping.ModuleId && mv.Version == payload.Version);

            if (existingVersion != null)
            {
                _logger.LogInformation("Version {Version} already exists for module {ModuleId}", payload.Version, mapping.ModuleId);
                var successResponse = new { message = "Version already exists", version = payload.Version };
                webhookRequest.StatusCode = 200;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(successResponse);
                return Ok(successResponse);
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

            var response = new
            {
                message = "Version created successfully",
                moduleId = mapping.ModuleId,
                version = payload.Version
            };
            webhookRequest.StatusCode = 200;
            webhookRequest.ResponseBody = JsonSerializer.Serialize(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing npm webhook");
            var errorResponse = new { error = "Internal server error" };
            webhookRequest.StatusCode = 500;
            webhookRequest.ResponseBody = JsonSerializer.Serialize(errorResponse);
            return StatusCode(500, errorResponse);
        }
        finally
        {
            stopwatch.Stop();
            webhookRequest.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
            _context.WebhookRequests.Add(webhookRequest);
            await _context.SaveChangesAsync();
        }
    }

    [HttpGet("test")]
    public IActionResult TestWebhook()
    {
        var npmSecret = _configuration["Webhooks:NpmSecret"];
        var nugetSecret = _configuration["Webhooks:NuGetSecret"];
        return Ok(new
        {
            npmWebhookConfigured = !string.IsNullOrEmpty(npmSecret),
            nugetWebhookConfigured = !string.IsNullOrEmpty(nugetSecret),
            message = "Webhook endpoints are configured"
        });
    }

    [HttpPost("nuget-registry")]
    public async Task<IActionResult> NuGetWebhook()
    {
        var stopwatch = Stopwatch.StartNew();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();
        
        var signature = Request.Headers["X-NuGet-Signature"].FirstOrDefault() ?? "";
        
        var webhookRequest = new WebhookRequest
        {
            Endpoint = "/api/webhooks/nuget-registry",
            RegistryType = "nuget",
            Payload = rawBody,
            Signature = signature,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var secret = _configuration["Webhooks:NuGetSecret"];

            webhookRequest.SignatureValid = _signatureValidator.ValidateNuGetSignature(rawBody, signature, secret ?? "");

            if (!webhookRequest.SignatureValid)
            {
                _logger.LogWarning("Invalid NuGet webhook signature");
                var errorResponse = new { error = "Invalid signature" };
                webhookRequest.StatusCode = 401;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(errorResponse);
                return Unauthorized(errorResponse);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var payload = JsonSerializer.Deserialize<NuGetWebhookPayload>(rawBody, options);

            if (payload == null)
            {
                _logger.LogWarning("Failed to parse NuGet webhook payload");
                var errorResponse = new { error = "Invalid payload" };
                webhookRequest.StatusCode = 400;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(errorResponse);
                return BadRequest(errorResponse);
            }

            webhookRequest.EventType = payload.Event;
            webhookRequest.PackageName = payload.PackageId;
            webhookRequest.PackageVersion = payload.Version;

            if (payload.Event != "package:publish" && payload.Event != "PackagePushed")
            {
                _logger.LogInformation("Ignoring NuGet webhook event: {Event}", payload.Event);
                var successResponse = new { message = "Event ignored", eventType = payload.Event };
                webhookRequest.StatusCode = 200;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(successResponse);
                return Ok(successResponse);
            }

            _logger.LogInformation("Received NuGet publish webhook for {Package} v{Version}", payload.PackageId, payload.Version);

            var mapping = await _context.PackageRegistryMappings
                .Include(m => m.Module)
                .FirstOrDefaultAsync(m => m.RegistryType == "nuget" && m.PackageName == payload.PackageId);

            if (mapping == null)
            {
                _logger.LogWarning("No module mapping found for NuGet package: {Package}", payload.PackageId);
                var errorResponse = new { error = "Package not registered", package = payload.PackageId };
                webhookRequest.StatusCode = 404;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(errorResponse);
                return NotFound(errorResponse);
            }

            var existingVersion = await _context.ModuleVersions
                .FirstOrDefaultAsync(mv => mv.ModuleId == mapping.ModuleId && mv.Version == payload.Version);

            if (existingVersion != null)
            {
                _logger.LogInformation("Version {Version} already exists for module {ModuleId}", payload.Version, mapping.ModuleId);
                var successResponse = new { message = "Version already exists", version = payload.Version };
                webhookRequest.StatusCode = 200;
                webhookRequest.ResponseBody = JsonSerializer.Serialize(successResponse);
                return Ok(successResponse);
            }

            var moduleVersion = new ModuleVersion
            {
                ModuleId = mapping.ModuleId,
                Version = payload.Version,
                ReleasedAt = payload.Published,
                ReleaseNotes = $"Published to NuGet.org",
                IsBreakingChange = IsBreakingChange(payload.Version),
                Changelog = payload.Metadata?.Urls?.PackageDownload ?? "",
                DemoCode = "",
                SupportedStacksJson = "[]"
            };

            _context.ModuleVersions.Add(moduleVersion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created module version {Version} for module {ModuleId}", payload.Version, mapping.ModuleId);

            var response = new
            {
                message = "Version created successfully",
                moduleId = mapping.ModuleId,
                version = payload.Version
            };
            webhookRequest.StatusCode = 200;
            webhookRequest.ResponseBody = JsonSerializer.Serialize(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NuGet webhook");
            var errorResponse = new { error = "Internal server error" };
            webhookRequest.StatusCode = 500;
            webhookRequest.ResponseBody = JsonSerializer.Serialize(errorResponse);
            return StatusCode(500, errorResponse);
        }
        finally
        {
            stopwatch.Stop();
            webhookRequest.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
            _context.WebhookRequests.Add(webhookRequest);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets webhook request history with filtering and pagination.
    /// Admin only endpoint.
    /// </summary>
    [HttpGet("history")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetWebhookHistory(
        [FromQuery] string? registryType = null,
        [FromQuery] string? packageName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? statusCode = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = _context.WebhookRequests.AsQueryable();

            if (!string.IsNullOrEmpty(registryType))
                query = query.Where(w => w.RegistryType == registryType);

            if (!string.IsNullOrEmpty(packageName))
                query = query.Where(w => w.PackageName == packageName);

            if (startDate.HasValue)
                query = query.Where(w => w.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(w => w.CreatedAt <= endDate.Value);

            if (statusCode.HasValue)
                query = query.Where(w => w.StatusCode == statusCode.Value);

            var total = await query.CountAsync();

            var webhooks = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new
                {
                    w.Id,
                    w.Endpoint,
                    w.RegistryType,
                    w.EventType,
                    w.PackageName,
                    w.PackageVersion,
                    w.IpAddress,
                    w.StatusCode,
                    w.SignatureValid,
                    w.ProcessingTimeMs,
                    w.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                webhooks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook history");
            return StatusCode(500, new { error = "Internal server error" });
        }
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

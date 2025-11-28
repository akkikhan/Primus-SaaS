using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using PrimusSaaS.Portal.Api.Services;


namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ApplicationsController : ControllerBase
{
    private readonly PortalDbContext _context;
        private readonly IEmailService _emailService;

    public ApplicationsController(PortalDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // GET: api/applications
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetApplications()
    {
        var applications = await _context.Applications
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                Name = a.Name,
                Stack = a.Stack.ToString(),
                PrimusClientId = a.PrimusClientId,
                Description = a.Description,
                OwnerEmail = a.Owner.Email,
                ModuleCount = a.ApplicationModules.Count,
                CreatedAt = a.CreatedAt,
                ClientSecretLastRotatedAt = a.ClientSecretLastRotatedAt,
                HasClientSecret = a.ClientSecretHash != string.Empty
            })
            .ToListAsync();

        return Ok(applications);
    }

    // GET: api/applications/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationDetailDto>> GetApplication(int id)
    {
        var application = await _context.Applications
            .Where(a => a.Id == id)
            .Select(a => new ApplicationDetailDto
            {
                Id = a.Id,
                Name = a.Name,
                Stack = a.Stack.ToString(),
                PrimusClientId = a.PrimusClientId,
                Description = a.Description,
                OwnerEmail = a.Owner.Email,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                ClientSecretLastRotatedAt = a.ClientSecretLastRotatedAt,
                HasClientSecret = a.ClientSecretHash != string.Empty,
                IntegratedModules = a.ApplicationModules.Select(am => new IntegratedModuleDto
                {
                    ModuleId = am.ModuleId,
                    ModuleName = am.Module.Name,
                    Version = am.ModuleVersion.Version,
                    LatestVersion = am.Module.Versions.OrderByDescending(v => v.ReleasedAt).First().Version,
                    VersionStatus = am.ModuleVersion.Version == am.Module.Versions.OrderByDescending(v => v.ReleasedAt).First().Version 
                        ? "UpToDate" 
                        : "UpdateAvailable",
                    IsBreakingChange = am.ModuleVersion.IsBreakingChange,
                    ReleaseNotes = am.ModuleVersion.ReleaseNotes,
                    Changelog = am.ModuleVersion.Changelog,
                    ReleasedAt = am.ModuleVersion.ReleasedAt,
                    ConfigJson = am.ConfigJson,
                    IntegratedAt = am.IntegratedAt
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (application == null)
        {
            return NotFound();
        }

        return Ok(application);
    }

    // POST: api/applications
    [HttpPost]
    public async Task<ActionResult<Application>> CreateApplication([FromBody] CreateApplicationRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // Parse stack enum (normalize frontend format)
        var normalizedStack = NormalizeStackString(request.Stack);
        if (!Enum.TryParse<AppStack>(normalizedStack, out var stack))
        {
            return BadRequest(new { message = "Invalid stack specified" });
        }

        // Auto-generate Primus Client ID + secret for tracking
        var primusClientId = GeneratePrimusClientId();
        var clientSecret = GenerateClientSecret();
        var secretHash = BCrypt.Net.BCrypt.HashPassword(clientSecret);

        var application = new Application
        {
            OwnerUserId = userId,
            Name = request.Name,
            Stack = stack,
            Description = request.Description,
            PrimusClientId = primusClientId,
            ClientSecretHash = secretHash,
            ClientSecretLastRotatedAt = DateTime.UtcNow
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        // Return DTO instead of entity
        var dto = new ApplicationDto
        {
            Id = application.Id,
            Name = application.Name,
            Stack = application.Stack.ToString(),
            PrimusClientId = application.PrimusClientId,
            Description = application.Description,
            OwnerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
            ModuleCount = 0,
            CreatedAt = application.CreatedAt,
            // ClientSecret = clientSecret, // REMOVED: Only sent via email
            ClientSecretLastRotatedAt = application.ClientSecretLastRotatedAt,
            HasClientSecret = true
        };

        // Send email notification to client about new application creation
        var recipientEmail = !string.IsNullOrWhiteSpace(request.ClientEmail) ? request.ClientEmail : User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        await _emailService.SendApplicationCreatedAsync(application, clientSecret, recipientEmail);
        return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, dto);
    }

    // POST: api/applications/5/modules
    [HttpPost("{applicationId}/modules")]
    public async Task<ActionResult<ApplicationModule>> IntegrateModule(int applicationId, [FromBody] IntegrateModuleRequest request)
    {
        var application = await _context.Applications
            .Include(a => a.Owner)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application == null)
        {
            return NotFound("Application not found");
        }

        var moduleVersion = await _context.ModuleVersions
            .Include(mv => mv.Module)
            .FirstOrDefaultAsync(mv => mv.ModuleId == request.ModuleId && mv.Id == request.ModuleVersionId);

        if (moduleVersion == null)
        {
            return NotFound("Module version not found");
        }

        // Check if module already integrated
        var existing = await _context.ApplicationModules
            .FirstOrDefaultAsync(am => am.ApplicationId == applicationId && am.ModuleId == request.ModuleId);

        var configJson = string.IsNullOrWhiteSpace(request.ConfigJson) ? "{}" : request.ConfigJson;

        if (existing != null)
        {
            // Update to new version
            existing.ModuleVersionId = request.ModuleVersionId;
            existing.ConfigJson = configJson;
        }
        else
        {
            // Add new integration
            var appModule = new ApplicationModule
            {
                ApplicationId = applicationId,
                ModuleId = request.ModuleId,
                ModuleVersionId = request.ModuleVersionId,
                ConfigJson = configJson
            };

            _context.ApplicationModules.Add(appModule);
            await _context.SaveChangesAsync();
            
            // Send email notification
            await _emailService.SendModuleAssignedAsync(application, moduleVersion);
        }

        if (existing != null)
        {
             await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Module integrated successfully" });
    }

    // DELETE: api/applications/{applicationId}/modules/{moduleId}
    [HttpDelete("{applicationId}/modules/{moduleId}")]
    public async Task<IActionResult> RemoveModule(int applicationId, int moduleId)
    {
        var appModule = await _context.ApplicationModules
            .FirstOrDefaultAsync(am => am.ApplicationId == applicationId && am.ModuleId == moduleId);

        if (appModule == null)
        {
            return NotFound("Module integration not found");
        }

        _context.ApplicationModules.Remove(appModule);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/applications/5/modules/3/version
    [HttpPost("{applicationId}/modules/{moduleId}/version")]
    public async Task<IActionResult> ChangeModuleVersion(int applicationId, int moduleId, [FromBody] ChangeVersionRequest request)
    {
        var appModule = await _context.ApplicationModules
            .Include(am => am.Application)
            .FirstOrDefaultAsync(am => am.ApplicationId == applicationId && am.ModuleId == moduleId);

        if (appModule == null)
        {
            return NotFound("Module not integrated with this application");
        }

        var newVersion = await _context.ModuleVersions
            .FirstOrDefaultAsync(mv => mv.ModuleId == moduleId && mv.Version == request.Version);

        if (newVersion == null)
        {
            return NotFound($"Version {request.Version} not found for this module");
        }

        appModule.ModuleVersionId = newVersion.Id;
        appModule.Application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = $"Module version updated to {request.Version}" });
    }

    // PUT: api/applications/5
    [HttpPut("{id}")]
    public async Task<ActionResult<ApplicationDto>> UpdateApplication(int id, [FromBody] UpdateApplicationRequest request)
    {
        var application = await _context.Applications
            .Include(a => a.Owner)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound();
        }

        // Update fields
        if (!string.IsNullOrEmpty(request.Name))
        {
            application.Name = request.Name;
        }

        // Stack is immutable after creation to avoid inconsistent integrations
        if (!string.IsNullOrEmpty(request.Stack) &&
            !string.Equals(application.Stack.ToString(), NormalizeStackString(request.Stack), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Technology stack cannot be changed after the application is created." });
        }

        if (request.Description != null)
        {
            application.Description = request.Description;
        }

        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Return updated DTO
        var dto = new ApplicationDto
        {
            Id = application.Id,
            Name = application.Name,
            Stack = application.Stack.ToString(),
            PrimusClientId = application.PrimusClientId,
            Description = application.Description,
            OwnerEmail = application.Owner.Email,
            ModuleCount = await _context.ApplicationModules.CountAsync(am => am.ApplicationId == id),
            CreatedAt = application.CreatedAt,
            ClientSecretLastRotatedAt = application.ClientSecretLastRotatedAt,
            HasClientSecret = !string.IsNullOrEmpty(application.ClientSecretHash)
        };

        return Ok(dto);
    }

    // DELETE: api/applications/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApplication(int id)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application == null)
        {
            return NotFound();
        }

        _context.Applications.Remove(application);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/credentials/rotate")]
    public async Task<ActionResult<ApplicationCredentialResponse>> RotateClientSecret(int id)
    {
        var application = await _context.Applications.FirstOrDefaultAsync(a => a.Id == id);
        if (application == null)
        {
            return NotFound();
        }

        var newSecret = GenerateClientSecret();
        application.ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(newSecret);
        application.ClientSecretLastRotatedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new ApplicationCredentialResponse
        {
            PrimusClientId = application.PrimusClientId,
            ClientSecret = newSecret,
            RotatedAt = application.ClientSecretLastRotatedAt ?? DateTime.UtcNow
        });
    }

    private string GeneratePrimusClientId()
    {
        // Format: PSP-CLI-000123
        var random = new Random();
        var number = random.Next(1, 999999);
        return $"PSP-CLI-{number:D6}";
    }

    private string GenerateClientSecret()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        var encoded = Convert.ToBase64String(buffer)
            .TrimEnd('=')
            .Replace('+', 'A')
            .Replace('/', 'B');
        return $"psp_{encoded}";
    }

    private string NormalizeStackString(string stack)
    {
        // Normalize frontend stack strings to match enum names
        var trimmed = stack.Replace(" ", "").Replace("-", "");
        return trimmed switch
        {
            "NodeJSNest" => "NodeJS",
            "TypeScriptLib" => "NodeJS", // treat TS client as NodeJS docs for now
            "PythonFastAPI" => "Python",
            _ => trimmed
        };
    }
}

public record ApplicationDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Stack { get; init; } = string.Empty;
    public string PrimusClientId { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string OwnerEmail { get; init; } = string.Empty;
    public int ModuleCount { get; init; }
    public DateTime CreatedAt { get; init; }
    // ClientSecret removed from DTO - only sent via email
    public DateTime? ClientSecretLastRotatedAt { get; init; }
    public bool HasClientSecret { get; init; }
}

public record ApplicationDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Stack { get; init; } = string.Empty;
    public string PrimusClientId { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string OwnerEmail { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ClientSecretLastRotatedAt { get; init; }
    public bool HasClientSecret { get; init; }
    public List<IntegratedModuleDto> IntegratedModules { get; init; } = new();
}

public record IntegratedModuleDto
{
    public int ModuleId { get; init; }
    public string ModuleName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string LatestVersion { get; init; } = string.Empty;
    public string VersionStatus { get; init; } = string.Empty; // "UpToDate" or "UpdateAvailable"
    public bool IsBreakingChange { get; init; }
    public string ReleaseNotes { get; init; } = string.Empty;
    public string Changelog { get; init; } = string.Empty;
    public DateTime ReleasedAt { get; init; }
    public string ConfigJson { get; init; } = string.Empty;
    public DateTime IntegratedAt { get; init; }
}

public record CreateApplicationRequest(string Name, string Stack, string? Description = null, string? ClientEmail = null);
public record UpdateApplicationRequest(string? Name = null, string? Stack = null, string? Description = null);
    public record IntegrateModuleRequest(int ModuleId, int ModuleVersionId, string ConfigJson = "{}");
public record ChangeVersionRequest(string Version);
public record ApplicationCredentialResponse
{
    public string PrimusClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public DateTime RotatedAt { get; init; }
}

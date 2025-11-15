using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using System.Security.Claims;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ApplicationsController : ControllerBase
{
    private readonly PortalDbContext _context;

    public ApplicationsController(PortalDbContext context)
    {
        _context = context;
    }

    // GET: api/applications
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetApplications()
    {
        var applications = await _context.Applications
            .Include(a => a.Owner)
            .Include(a => a.ApplicationModules)
                .ThenInclude(am => am.Module)
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                Name = a.Name,
                Stack = a.Stack.ToString(),
                PrimusClientId = a.PrimusClientId,
                OwnerEmail = a.Owner.Email,
                ModuleCount = a.ApplicationModules.Count,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(applications);
    }

    // GET: api/applications/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationDetailDto>> GetApplication(int id)
    {
        var application = await _context.Applications
            .Include(a => a.Owner)
            .Include(a => a.ApplicationModules)
                .ThenInclude(am => am.Module)
            .Include(a => a.ApplicationModules)
                .ThenInclude(am => am.ModuleVersion)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound();
        }

        return Ok(new ApplicationDetailDto
        {
            Id = application.Id,
            Name = application.Name,
            Stack = application.Stack.ToString(),
            PrimusClientId = application.PrimusClientId,
            OwnerEmail = application.Owner.Email,
            CreatedAt = application.CreatedAt,
            IntegratedModules = application.ApplicationModules.Select(am => new IntegratedModuleDto
            {
                ModuleId = am.ModuleId,
                ModuleName = am.Module.Name,
                Version = am.ModuleVersion.Version,
                ConfigJson = am.ConfigJson,
                IntegratedAt = am.IntegratedAt
            }).ToList()
        });
    }

    // POST: api/applications
    [HttpPost]
    public async Task<ActionResult<Application>> CreateApplication([FromBody] CreateApplicationRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var application = new Application
        {
            OwnerUserId = userId,
            Name = request.Name,
            Stack = Enum.Parse<AppStack>(request.Stack),
            PrimusClientId = GenerateClientId()
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, application);
    }

    // POST: api/applications/5/modules
    [HttpPost("{applicationId}/modules")]
    public async Task<ActionResult<ApplicationModule>> IntegrateModule(int applicationId, [FromBody] IntegrateModuleRequest request)
    {
        var application = await _context.Applications.FindAsync(applicationId);
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

        if (existing != null)
        {
            // Update to new version
            existing.ModuleVersionId = request.ModuleVersionId;
            existing.ConfigJson = request.ConfigJson;
        }
        else
        {
            // Add new integration
            var appModule = new ApplicationModule
            {
                ApplicationId = applicationId,
                ModuleId = request.ModuleId,
                ModuleVersionId = request.ModuleVersionId,
                ConfigJson = request.ConfigJson
            };
            _context.ApplicationModules.Add(appModule);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Module integrated successfully" });
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

    private string GenerateClientId()
    {
        return $"primus_{Guid.NewGuid():N}";
    }
}

public record ApplicationDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Stack { get; init; } = string.Empty;
    public string PrimusClientId { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
    public int ModuleCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ApplicationDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Stack { get; init; } = string.Empty;
    public string PrimusClientId { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public List<IntegratedModuleDto> IntegratedModules { get; init; } = new();
}

public record IntegratedModuleDto
{
    public int ModuleId { get; init; }
    public string ModuleName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string ConfigJson { get; init; } = string.Empty;
    public DateTime IntegratedAt { get; init; }
}

public record CreateApplicationRequest(string Name, string Stack);
public record IntegrateModuleRequest(int ModuleId, int ModuleVersionId, string ConfigJson);

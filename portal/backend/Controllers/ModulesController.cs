using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ModulesController : ControllerBase
{
    private readonly PortalDbContext _context;

    public ModulesController(PortalDbContext context)
    {
        _context = context;
    }

    // GET: api/modules
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ModuleDto>>> GetModules()
    {
        var modules = await _context.Modules
            .Include(m => m.Versions)
            .Include(m => m.ApplicationModules)
            .Select(m => new ModuleDto
            {
                Id = m.Id,
                Name = m.Name,
                ModuleKey = m.ModuleKey,
                Description = m.Description,
                LatestVersion = m.Versions.OrderByDescending(v => v.ReleasedAt).FirstOrDefault()!.Version,
                LatestReleasedAt = m.Versions.OrderByDescending(v => v.ReleasedAt).FirstOrDefault()?.ReleasedAt ?? DateTime.UtcNow,
                ModuleVersions = m.Versions
                    .OrderByDescending(v => v.ReleasedAt)
                    .Select(v => new VersionDto
                    {
                        Id = v.Id,
                        Version = v.Version,
                        IsBreakingChange = v.IsBreakingChange,
                        ReleaseNotes = v.ReleaseNotes,
                        Changelog = v.Changelog,
                        DemoCode = v.DemoCode,
                        SupportedStacks = System.Text.Json.JsonSerializer.Deserialize<string[]>(v.SupportedStacksJson) ?? Array.Empty<string>(),
                        ReleasedAt = v.ReleasedAt
                    }).ToList(),
                TotalVersions = m.Versions.Count,
                UsageCount = m.ApplicationModules.Count,
                Status = "Active"
            })
            .ToListAsync();

        return Ok(modules);
    }

    // GET: api/modules/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ModuleDetailDto>> GetModule(int id)
    {
        var module = await _context.Modules
            .Include(m => m.Versions)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (module == null)
        {
            return NotFound();
        }

        return Ok(new ModuleDetailDto
        {
            Id = module.Id,
            Name = module.Name,
            ModuleKey = module.ModuleKey,
            Description = module.Description,
            Versions = module.Versions.Select(v => new VersionDto
            {
                Id = v.Id,
                Version = v.Version,
                IsBreakingChange = v.IsBreakingChange,
                ReleaseNotes = v.ReleaseNotes,
                Changelog = v.Changelog,
                DemoCode = v.DemoCode,
                SupportedStacks = System.Text.Json.JsonSerializer.Deserialize<string[]>(v.SupportedStacksJson) ?? Array.Empty<string>(),
                ReleasedAt = v.ReleasedAt
            }).OrderByDescending(v => v.ReleasedAt).ToList()
        });
    }

    // GET: api/modules/{id}/versions
    [HttpGet("{id}/versions")]
    public async Task<ActionResult<IEnumerable<VersionDto>>> GetModuleVersions(int id)
    {
        var exists = await _context.Modules.AnyAsync(m => m.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        var versions = await _context.ModuleVersions
            .Where(v => v.ModuleId == id)
            .OrderByDescending(v => v.ReleasedAt)
            .Select(v => new VersionDto
            {
                Id = v.Id,
                Version = v.Version,
                IsBreakingChange = v.IsBreakingChange,
                ReleaseNotes = v.ReleaseNotes,
                Changelog = v.Changelog,
                DemoCode = v.DemoCode,
                SupportedStacks = System.Text.Json.JsonSerializer.Deserialize<string[]>(v.SupportedStacksJson) ?? Array.Empty<string>(),
                ReleasedAt = v.ReleasedAt
            })
            .ToListAsync();

        return Ok(versions);
    }

    // POST: api/modules
    [HttpPost]
    public async Task<ActionResult<Module>> CreateModule([FromBody] CreateModuleRequest request)
    {
        // Validate uniqueness for name and key to give a clearer error than DB constraint
        var duplicate = await _context.Modules
            .AnyAsync(m => m.Name == request.Name || m.ModuleKey == request.ModuleKey);
        if (duplicate)
        {
            return BadRequest(new { message = "Module name and module key must be unique." });
        }

        var module = new Module
        {
            Name = request.Name,
            ModuleKey = request.ModuleKey,
            Description = request.Description
        };

        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetModule), new { id = module.Id }, module);
    }

    // POST: api/modules/5/versions
    [HttpPost("{moduleId}/versions")]
    public async Task<ActionResult<ModuleVersion>> CreateVersion(int moduleId, [FromBody] CreateVersionRequest request)
    {
        var module = await _context.Modules.FindAsync(moduleId);
        if (module == null)
        {
            return NotFound();
        }

        // Prevent duplicate version identifiers per module
        var versionExists = await _context.ModuleVersions
            .AnyAsync(v => v.ModuleId == moduleId && v.Version == request.Version);
        if (versionExists)
        {
            return BadRequest(new { message = $"Version {request.Version} already exists for this module." });
        }

        var version = new ModuleVersion
        {
            ModuleId = moduleId,
            Version = request.Version,
            IsBreakingChange = request.IsBreakingChange,
            ReleaseNotes = request.ReleaseNotes,
            Changelog = request.Changelog,
            DemoCode = request.DemoCode,
            SupportedStacksJson = System.Text.Json.JsonSerializer.Serialize(request.SupportedStacks),
            ReleasedAt = request.ReleasedAt ?? DateTime.UtcNow
        };

        _context.ModuleVersions.Add(version);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetModule), new { id = moduleId }, version);
    }

    // PATCH: api/modules/{moduleId}/versions/{version}/publish
    [HttpPatch("{moduleId}/versions/{version}/publish")]
    public async Task<IActionResult> PublishVersion(int moduleId, string version)
    {
        var moduleVersion = await _context.ModuleVersions
            .FirstOrDefaultAsync(mv => mv.ModuleId == moduleId && mv.Version == version);

        if (moduleVersion == null)
        {
            return NotFound();
        }

        moduleVersion.ReleasedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Version {version} published", releasedAt = moduleVersion.ReleasedAt });
    }

    // PUT: api/modules/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateModule(int id, [FromBody] UpdateModuleRequest request)
    {
        var module = await _context.Modules.FindAsync(id);
        if (module == null)
        {
            return NotFound();
        }

        // Validate uniqueness before applying updates
        var duplicate = await _context.Modules
            .AnyAsync(m => m.Id != id && (m.Name == request.Name || m.ModuleKey == request.ModuleKey));
        if (duplicate)
        {
            return BadRequest(new { message = "Module name and module key must be unique." });
        }

        module.Name = request.Name;
        module.ModuleKey = request.ModuleKey;
        module.Description = request.Description;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/modules/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteModule(int id)
    {
        var module = await _context.Modules.FindAsync(id);
        if (module == null)
        {
            return NotFound();
        }

        _context.Modules.Remove(module);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record ModuleDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ModuleKey { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string LatestVersion { get; init; } = string.Empty;
    public DateTime LatestReleasedAt { get; init; }
    public List<VersionDto> ModuleVersions { get; init; } = new();
    public int TotalVersions { get; init; }
    public int UsageCount { get; init; }
    public string Status { get; init; } = "Active";
}

public record ModuleDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ModuleKey { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<VersionDto> Versions { get; init; } = new();
}

public record VersionDto
{
    public int Id { get; init; }
    public string Version { get; init; } = string.Empty;
    public bool IsBreakingChange { get; init; }
    public string ReleaseNotes { get; init; } = string.Empty;
    public string Changelog { get; init; } = string.Empty;
    public string DemoCode { get; init; } = string.Empty;
    public string[] SupportedStacks { get; init; } = Array.Empty<string>();
    public DateTime ReleasedAt { get; init; }
}

public record CreateModuleRequest(string Name, string ModuleKey, string Description);
public record UpdateModuleRequest(string Name, string ModuleKey, string Description);
public record CreateVersionRequest(string Version, bool IsBreakingChange, string ReleaseNotes, string Changelog, string DemoCode, string[] SupportedStacks, DateTime? ReleasedAt = null);

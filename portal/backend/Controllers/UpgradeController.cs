using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/upgrade")]
[Authorize(Roles = "Admin")]
public class UpgradeController : ControllerBase
{
    private readonly PortalDbContext _context;

    public UpgradeController(PortalDbContext context)
    {
        _context = context;
    }

    // GET: api/upgrade/overview
    [HttpGet("overview")]
    public async Task<ActionResult<IEnumerable<ApplicationUpgradeDto>>> GetUpgradeOverview()
    {
        var apps = await _context.Applications
            .Include(a => a.ApplicationModules)
                .ThenInclude(am => am.Module)
            .Include(a => a.ApplicationModules)
                .ThenInclude(am => am.ModuleVersion)
            .Select(a => new ApplicationUpgradeDto
            {
                ApplicationId = a.Id,
                ApplicationName = a.Name,
                Stack = a.Stack.ToString(),
                PrimusClientId = a.PrimusClientId,
                Modules = a.ApplicationModules.Select(am =>
                {
                    var latestVersion = am.Module.Versions.OrderByDescending(v => v.ReleasedAt).FirstOrDefault();
                    return new ModuleUpgradeDto
                    {
                        ModuleId = am.ModuleId,
                        ModuleName = am.Module.Name,
                        CurrentVersion = am.ModuleVersion.Version,
                        LatestVersion = latestVersion?.Version ?? am.ModuleVersion.Version,
                        CurrentReleaseNotes = am.ModuleVersion.ReleaseNotes,
                        CurrentChangelog = am.ModuleVersion.Changelog,
                        LatestReleaseNotes = latestVersion?.ReleaseNotes ?? string.Empty,
                        LatestChangelog = latestVersion?.Changelog ?? string.Empty,
                        Status = latestVersion == null || latestVersion.Version == am.ModuleVersion.Version
                            ? "UpToDate"
                            : "UpdateAvailable",
                        IsBreakingChange = latestVersion?.IsBreakingChange ?? am.ModuleVersion.IsBreakingChange
                    };
                }).ToList()
            })
            .ToListAsync();

        return Ok(apps);
    }

    // POST: api/upgrade/applications/{applicationId}/modules/{moduleId}/upgrade
    [HttpPost("applications/{applicationId}/modules/{moduleId}/upgrade")]
    public async Task<IActionResult> UpgradeModuleToLatest(int applicationId, int moduleId)
    {
        var appModule = await _context.ApplicationModules
            .Include(am => am.Module)
            .FirstOrDefaultAsync(am => am.ApplicationId == applicationId && am.ModuleId == moduleId);

        if (appModule == null)
        {
            return NotFound("Module integration not found");
        }

        // Get latest version by release date
        var latestVersion = await _context.ModuleVersions
            .Where(mv => mv.ModuleId == moduleId)
            .OrderByDescending(mv => mv.ReleasedAt)
            .FirstOrDefaultAsync();

        if (latestVersion == null)
        {
            return BadRequest(new { message = "No versions published for this module" });
        }

        appModule.ModuleVersionId = latestVersion.Id;
        appModule.Application.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Module upgraded to latest version", latestVersion = latestVersion.Version });
    }
}

public record ApplicationUpgradeDto
{
    public int ApplicationId { get; init; }
    public string ApplicationName { get; init; } = string.Empty;
    public string Stack { get; init; } = string.Empty;
    public string PrimusClientId { get; init; } = string.Empty;
    public List<ModuleUpgradeDto> Modules { get; init; } = new();
}

public record ModuleUpgradeDto
{
    public int ModuleId { get; init; }
    public string ModuleName { get; init; } = string.Empty;
    public string CurrentVersion { get; init; } = string.Empty;
    public string LatestVersion { get; init; } = string.Empty;
    public string CurrentReleaseNotes { get; init; } = string.Empty;
    public string CurrentChangelog { get; init; } = string.Empty;
    public string LatestReleaseNotes { get; init; } = string.Empty;
    public string LatestChangelog { get; init; } = string.Empty;
    public string Status { get; init; } = "UpToDate";
    public bool IsBreakingChange { get; init; }
}


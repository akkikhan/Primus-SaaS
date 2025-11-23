using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;

namespace PrimusSaaS.Portal.Api.Services;

/// <summary>
/// Central place to determine which applications own which modules, so notifications stay targeted.
/// </summary>
public class ModuleOwnershipService : IModuleOwnershipService
{
    private readonly PortalDbContext _context;

    public ModuleOwnershipService(PortalDbContext context)
    {
        _context = context;
    }

    public Task<List<Application>> GetApplicationsForModuleAsync(int moduleId)
    {
        return _context.ApplicationModules
            .Where(am => am.ModuleId == moduleId)
            .Include(am => am.Application)
                .ThenInclude(a => a.Owner)
            .Select(am => am.Application)
            .Distinct()
            .ToListAsync();
    }
}

using PrimusSaaS.Portal.Api.Models;

namespace PrimusSaaS.Portal.Api.Services;

public interface IModuleOwnershipService
{
    Task<List<Application>> GetApplicationsForModuleAsync(int moduleId);
}

using PrimusSaaS.Portal.Api.Models;
using System.Threading.Tasks;

namespace PrimusSaaS.Portal.Api.Services;

public interface IEmailService
{
    Task SendApplicationCreatedAsync(Application app, string clientSecret, string? recipientEmail = null);
    Task SendModuleAssignedAsync(Application app, ModuleVersion version);
    Task SendVersionPublishedAsync(Application app, ModuleVersion version);
}

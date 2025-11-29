using System.Threading.Tasks;

namespace PrimusSaaS.Notifications.Abstractions;

public interface ITemplateService
{
    /// <summary>
    /// Renders a template for a specific notification type and channel.
    /// </summary>
    /// <param name="notificationType">The type of notification (e.g. "welcome").</param>
    /// <param name="channel">The channel (e.g. "Email").</param>
    /// <param name="model">The data model.</param>
    /// <returns>The rendered content (e.g. HTML body).</returns>
    Task<string> RenderAsync(string notificationType, string channel, object model);
}

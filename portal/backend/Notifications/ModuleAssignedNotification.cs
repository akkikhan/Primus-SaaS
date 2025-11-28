using System.Collections.Generic;
using Primus.Notifications.Abstractions;

namespace PrimusSaaS.Portal.Api.Notifications;

public class ModuleAssignedNotification : INotification
{
    public string Type => "ModuleAssigned";
    public object Data { get; }
    public IEnumerable<string> Channels => new[] { "Email" };
    public Recipient Recipient { get; }

    public ModuleAssignedNotification(string recipientEmail, string moduleName, string version, string appName, string installCommand, string docLink, string stack, string docsBaseUrl)
    {
        Data = new
        {
            ModuleName = moduleName,
            Version = version,
            AppName = appName,
            InstallCommand = installCommand,
            DocLink = docLink,
            Stack = stack,
            DocsBaseUrl = docsBaseUrl
        };

        Recipient = new Recipient
        {
            Email = recipientEmail
        };
    }
}

using System.Collections.Generic;
using Primus.Notifications.Abstractions;

namespace PrimusSaaS.Portal.Api.Notifications;

public class VersionPublishedNotification : INotification
{
    public string Type => "VersionPublished";
    public object Data { get; }
    public IEnumerable<string> Channels => new[] { "Email" };
    public Recipient Recipient { get; }

    public VersionPublishedNotification(string recipientEmail, string version, string moduleName, string npmPackageName, string nugetPackageName, string releaseNotes, string changelog, bool isBreakingChange)
    {
        Data = new
        {
            Version = version,
            ModuleName = moduleName,
            NpmPackageName = npmPackageName,
            NugetPackageName = nugetPackageName,
            ReleaseNotes = releaseNotes,
            Changelog = changelog,
            IsBreakingChange = isBreakingChange
        };

        Recipient = new Recipient
        {
            Email = recipientEmail
        };
    }
}

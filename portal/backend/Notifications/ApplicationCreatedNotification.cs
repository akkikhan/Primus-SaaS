using System.Collections.Generic;
using PrimusSaaS.Notifications.Abstractions;

namespace PrimusSaaS.Portal.Api.Notifications;

public class ApplicationCreatedNotification : INotification
{
    public string Type => "ApplicationCreated";
    public object Data { get; }
    public IEnumerable<string> Channels => new[] { "Email" };
    public Recipient Recipient { get; }

    public ApplicationCreatedNotification(string appName, string clientId, string recipientEmail)
    {
        Data = new
        {
            AppName = appName,
            ClientId = clientId
        };
        
        Recipient = new Recipient
        {
            Email = recipientEmail
        };
    }
}

using System.Collections.Generic;
using Primus.Notifications.Abstractions;

namespace PrimusNotificationTest;

public class WelcomeNotification : INotification
{
    public string Type => "Welcome";
    public object Data { get; }
    public IEnumerable<string> Channels => new[] { "Email", "Console" };
    public Recipient Recipient { get; }

    public WelcomeNotification(string name, string email)
    {
        Data = new { Name = name };
        Recipient = new Recipient
        {
            Name = name,
            Email = email
        };
    }
}

using System.Linq;
using System.Text.Json;
using PrimusSaaS.Notifications.Abstractions;

namespace PrimusSaaS.Notifications.Core;

internal sealed record QueuedNotificationEnvelope
{
    public string Type { get; init; } = string.Empty;
    public JsonElement Data { get; init; }
    public Recipient Recipient { get; init; } = new();
    public string[] Channels { get; init; } = Array.Empty<string>();

    public static QueuedNotificationEnvelope From(INotification notification, JsonSerializerOptions serializerOptions)
    {
        var dataJson = JsonSerializer.SerializeToElement(notification.Data, serializerOptions);
        return new QueuedNotificationEnvelope
        {
            Type = notification.Type,
            Data = dataJson,
            Recipient = notification.Recipient,
            Channels = notification.Channels?.ToArray() ?? Array.Empty<string>()
        };
    }

    public INotification ToNotification()
    {
        return new BasicNotification(Type, Data, Recipient, Channels);
    }
}

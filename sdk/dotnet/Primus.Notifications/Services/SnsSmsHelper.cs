using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace PrimusSaaS.Notifications.Services;

/// <summary>
/// Convenience helper to send SMS via AWS SNS without crafting a PublishRequest manually.
/// </summary>
public static class SnsSmsHelper
{
    public static Task<PublishResponse> SendSmsAsync(
        IAmazonSimpleNotificationService snsClient,
        string phoneNumber,
        string message,
        string smsType = "Transactional",
        string? senderId = null,
        CancellationToken cancellationToken = default)
    {
        var attributes = new Dictionary<string, MessageAttributeValue>
        {
            ["AWS.SNS.SMS.SMSType"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = smsType
            }
        };

        if (!string.IsNullOrWhiteSpace(senderId))
        {
            attributes["AWS.SNS.SMS.SenderID"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = senderId
            };
        }

        var request = new PublishRequest
        {
            PhoneNumber = phoneNumber,
            Message = message,
            MessageAttributes = attributes
        };

        return snsClient.PublishAsync(request, cancellationToken);
    }
}

using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Primus.Notifications.Abstractions;
using Primus.Notifications.Core;
using Xunit;

namespace Primus.Notifications.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task SendAsync_DispatchesRegisteredChannels_AndContinuesAfterFailures()
    {
        var recipient = new Recipient { Email = "test@example.com" };
        var successChannel = new RecordingChannel("Email");
        var failingChannel = new FailingChannel("Logger");
        var service = new NotificationService(
            new IChannel[] { successChannel, failingChannel },
            NullLogger<NotificationService>.Instance);

        var notification = new TestNotification(
            "Welcome",
            new { Name = "Ada" },
            recipient,
            "Email",
            "Logger",
            "Missing");

        await service.SendAsync(notification);

        Assert.Equal(1, successChannel.SendCount);
        Assert.Equal(1, failingChannel.Attempts);
    }

    [Fact]
    public async Task SendEmailAsync_BuildsDirectEmailNotification()
    {
        var channel = new CapturingChannel("Email");
        var service = new NotificationService(
            new IChannel[] { channel },
            NullLogger<NotificationService>.Instance);

        await service.SendEmailAsync("user@example.com", "Welcome", "Body", "Ada Lovelace");

        var notification = Assert.IsType<BasicNotification>(channel.LastNotification);
        Assert.Equal("primus.email.direct", notification.Type);
        Assert.Equal("Email", Assert.Single(notification.Channels));
        Assert.Equal("user@example.com", notification.Recipient.Email);
        Assert.Equal("Ada Lovelace", notification.Recipient.Name);
        var payload = Assert.IsType<DirectEmailContent>(notification.Data);
        Assert.Equal("Welcome", payload.Subject);
        Assert.Equal("Body", payload.Body);
    }

    [Fact]
    public async Task SendSmsAsync_BuildsDirectSmsNotification()
    {
        var channel = new CapturingChannel("Sms");
        var service = new NotificationService(
            new IChannel[] { channel },
            NullLogger<NotificationService>.Instance);

        await service.SendSmsAsync("+15551234567", "Hello!");

        var notification = Assert.IsType<BasicNotification>(channel.LastNotification);
        Assert.Equal("primus.sms.direct", notification.Type);
        Assert.Equal("Sms", Assert.Single(notification.Channels));
        Assert.Equal("+15551234567", notification.Recipient.PhoneNumber);
        var payload = Assert.IsType<DirectSmsContent>(notification.Data);
        Assert.Equal("Hello!", payload.Message);
    }
}

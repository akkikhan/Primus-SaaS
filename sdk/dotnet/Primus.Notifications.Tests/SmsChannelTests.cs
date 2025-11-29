using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Channels.Sms;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Core;
using Xunit;

namespace PrimusSaaS.Notifications.Tests;

public class SmsChannelTests
{
    private static ITemplateService EmptyTemplate() => new StubTemplateService(string.Empty);

    [Fact]
    public async Task SmsChannel_UsesDirectMessage_WhenTemplateEmpty()
    {
        var sender = new RecordingSmsSender();
        var channel = new SmsChannel(
            sender,
            EmptyTemplate(),
            Options.Create(new SmsOptions()),
            NullLogger<SmsChannel>.Instance);

        var notification = new TestNotification(
            "SmsTest",
            new DirectSmsContent("Hello SMS"),
            new Recipient { PhoneNumber = "+15551234567" },
            "Sms");

        await channel.SendAsync(notification);

        Assert.Equal(1, sender.SendCount);
        Assert.Equal("+15551234567", sender.LastTo);
        Assert.Equal("Hello SMS", sender.LastMessage);
    }

    [Fact]
    public async Task SmsChannel_UsesTemplate_WhenAvailable()
    {
        var sender = new RecordingSmsSender();
        var channel = new SmsChannel(
            sender,
            new StubTemplateService("templated message"),
            Options.Create(new SmsOptions()),
            NullLogger<SmsChannel>.Instance);

        var notification = new TestNotification(
            "SmsTest",
            new { Message = "ignored" },
            new Recipient { PhoneNumber = "+15550001111" },
            "Sms");

        await channel.SendAsync(notification);

        Assert.Equal("templated message", sender.LastMessage);
    }

    [Fact]
    public async Task SmsChannel_DirectMessage_BypassesTemplateResolution()
    {
        var sender = new RecordingSmsSender();
        var template = new CountingTemplateService();
        var channel = new SmsChannel(
            sender,
            template,
            Options.Create(new SmsOptions()),
            NullLogger<SmsChannel>.Instance);

        var notification = new TestNotification(
            BasicNotification.DirectSmsType,
            new DirectSmsContent("Hello SMS"),
            new Recipient { PhoneNumber = "+15551234567" },
            "Sms");

        await channel.SendAsync(notification);

        Assert.Equal(0, template.CallCount);
        Assert.Equal("Hello SMS", sender.LastMessage);
    }

    private sealed class StubTemplateService : ITemplateService
    {
        private readonly string _value;

        public StubTemplateService(string value)
        {
            _value = value;
        }

        public Task<string> RenderAsync(string notificationType, string channel, object model)
        {
            return Task.FromResult(_value);
        }
    }

    private sealed class CountingTemplateService : ITemplateService
    {
        public int CallCount { get; private set; }

        public Task<string> RenderAsync(string notificationType, string channel, object model)
        {
            CallCount++;
            return Task.FromResult(string.Empty);
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Services;
using Xunit;

namespace PrimusSaaS.Notifications.Tests;

public class NotificationHealthServiceTests
{
    [Fact]
    public async Task Health_ReturnsNotConfigured_WhenNoChannels()
    {
        var service = new NotificationHealthService(
            new List<IChannel>(),
            NullLogger<NotificationHealthService>.Instance,
            new FakeProvider());

        var snapshot = await service.GetChannelHealthAsync();

        Assert.Equal("not_configured", snapshot.Channels["email"]);
        Assert.Equal("not_configured", snapshot.Channels["sms"]);
    }

    [Fact]
    public async Task Health_ReturnsConfigured_WhenValidOptions()
    {
        var smtp = Options.Create(new SmtpOptions { Host = "h", Port = 25, FromAddress = "a@b.com" });
        var twilio = Options.Create(new TwilioOptions
        {
            AccountSid = "ACXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            AuthToken = "token",
            FromNumber = "+15551234567"
        });

        var service = new NotificationHealthService(
            new List<IChannel> { new FakeChannel("Email"), new FakeChannel("Sms") },
            NullLogger<NotificationHealthService>.Instance,
            new FakeProvider(smtp, twilio));

        var snapshot = await service.GetChannelHealthAsync();

        Assert.Equal("configured", snapshot.Channels["email"]);
        Assert.Equal("configured", snapshot.Channels["sms"]);
    }

    private sealed class FakeProvider : System.IServiceProvider
    {
        private readonly IOptions<SmtpOptions>? _smtp;
        private readonly IOptions<TwilioOptions>? _twilio;

        public FakeProvider(IOptions<SmtpOptions>? smtp = null, IOptions<TwilioOptions>? twilio = null)
        {
            _smtp = smtp;
            _twilio = twilio;
        }

        public object? GetService(System.Type serviceType)
        {
            if (serviceType == typeof(IOptions<SmtpOptions>)) return _smtp;
            if (serviceType == typeof(IOptions<TwilioOptions>)) return _twilio;
            return null;
        }
    }

    private sealed class FakeChannel : IChannel
    {
        public FakeChannel(string name) => Name = name;
        public string Name { get; }
        public Task SendAsync(INotification notification, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

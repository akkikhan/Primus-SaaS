using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;
using Xunit;

namespace PrimusSaaS.Notifications.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void UseSms_CustomSender_IsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITemplateService>(new StubTemplateService(string.Empty));

        services.AddPrimusNotifications(builder =>
        {
            builder.UseSms<CustomSmsSender>();
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISmsSender>();
        var channel = provider.GetServices<IChannel>().Single(c => c.Name == "Sms");

        Assert.IsType<CustomSmsSender>(sender);
        Assert.IsType<Channels.Sms.SmsChannel>(channel);
    }

    private sealed class CustomSmsSender : ISmsSender
    {
        public Task SendAsync(string to, string message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
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
}

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Channels.Sms;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Core;
using Xunit;

namespace PrimusSaaS.Notifications.Tests;

public class TwilioValidationTests
{
    [Fact]
    public void NotificationService_Resolves_WhenTwilioConfigMissing_AndSmsUnused()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<SmsOptions>(_ => { });
        services.AddSingleton<ITemplateService>(new InMemoryTemplateServiceStub(string.Empty));

        services.AddPrimusNotifications(builder =>
        {
            builder.UseSmtp(opts =>
            {
                opts.Host = "smtp.test";
                opts.Port = 25;
                opts.FromAddress = "noreply@test.com";
            });

            // Simulate an environment where Twilio is not configured (email-only usage).
            builder.UseTwilio(opts => { });
        });

        var provider = services.BuildServiceProvider();

        var exception = Record.Exception(() => provider.GetRequiredService<NotificationService>());

        Assert.Null(exception);
    }

    [Fact]
    public async Task TwilioSender_ThrowsHelpfulError_WhenNotConfigured()
    {
        var handler = new TestHandler();
        var httpClient = new HttpClient(handler);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<TwilioSmsSender>();

        var sender = new TwilioSmsSender(httpClient, Options.Create(new TwilioOptions()), logger);

        var ex = await Assert.ThrowsAsync<TwilioSmsException>(() =>
            sender.SendAsync("+15551234567", "Hello"));

        Assert.Contains("Twilio is not configured", ex.Message);
        Assert.Equal(0, handler.RequestCount);
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

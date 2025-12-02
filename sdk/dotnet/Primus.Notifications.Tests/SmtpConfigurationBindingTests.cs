using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Configuration;
using Xunit;

namespace PrimusSaaS.Notifications.Tests;

public class SmtpConfigurationBindingTests
{
    [Fact]
    public void UseSmtp_BindsFromConfigurationSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Host"] = "smtp.example.com",
                ["Port"] = "2525",
                ["Username"] = "user",
                ["Password"] = "pass",
                ["EnableSsl"] = "true",
                ["FromAddress"] = "no-reply@example.com",
                ["FromName"] = "Notifications Test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPrimusNotifications(builder =>
        {
            builder.UseSmtp(configuration);
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SmtpOptions>>().Value;

        Assert.Equal("smtp.example.com", options.Host);
        Assert.Equal(2525, options.Port);
        Assert.Equal("no-reply@example.com", options.FromAddress);
        Assert.Equal("Notifications Test", options.FromName);
    }
}

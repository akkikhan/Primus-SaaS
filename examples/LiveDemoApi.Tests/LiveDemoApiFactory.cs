using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LiveDemoApi.Tests;

/// <summary>
/// Custom WebApplicationFactory for Golden Path integration tests.
/// Configures the test server with test-specific settings.
/// </summary>
public class LiveDemoApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override with test-specific configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Use test-specific Application Insights (disabled for tests)
                ["PrimusLogging:ApplicationInsights:Enabled"] = "false",
                
                // Ensure logging is enabled for test verification
                ["PrimusLogging:Targets:Console:Enabled"] = "true",
                ["PrimusLogging:MinimumLevel"] = "Debug",
                
                // Disable external notification providers for tests
                ["Notifications:Smtp:Host"] = "localhost",
                ["Notifications:Twilio:ValidateOnStartup"] = "false",
            });
        });

        builder.UseEnvironment("Development");
    }
}

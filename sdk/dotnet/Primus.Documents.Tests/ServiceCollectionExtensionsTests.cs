using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Primus.Documents.LinkStore;
using Primus.Documents.SelfTest;
using Xunit;

namespace Primus.Documents.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPrimusDocumentRenderer_WithAction_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPrimusDocumentRenderer(opts =>
        {
            opts.MaxContentLength = 10000;
            opts.BrandName = "Test Brand";
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IDocumentRenderer>().Should().NotBeNull();
        provider.GetService<IDocumentLinkStore>().Should().NotBeNull();
        provider.GetService<IDocumentRendererSelfTest>().Should().NotBeNull();
    }

    [Fact]
    public void AddPrimusDocumentRenderer_WithConfiguration_BindsOptions()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["PrimusDocuments:MaxContentLength"] = "15000",
            ["PrimusDocuments:BrandName"] = "Config Brand",
            ["PrimusDocuments:SelfTestEnabled"] = "false",
            ["PrimusDocuments:LinkTtl"] = "00:05:00"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPrimusDocumentRenderer(config, "PrimusDocuments");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DocumentRendererOptions>>();

        // Assert
        options.Value.MaxContentLength.Should().Be(15000);
        options.Value.BrandName.Should().Be("Config Brand");
        options.Value.SelfTestEnabled.Should().BeFalse();
        options.Value.LinkTtl.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AddPrimusDocumentRenderer_ServicesAreSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPrimusDocumentRenderer();

        var provider = services.BuildServiceProvider();

        // Act
        var renderer1 = provider.GetService<IDocumentRenderer>();
        var renderer2 = provider.GetService<IDocumentRenderer>();

        // Assert
        renderer1.Should().BeSameAs(renderer2);
    }
}

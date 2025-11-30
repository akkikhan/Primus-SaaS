using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Primus.Documents.LinkStore;
using Primus.Documents.SelfTest;

namespace Primus.Documents;

/// <summary>
/// Extension methods for registering Primus Document Renderer services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Primus Document Renderer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPrimusDocumentRenderer(
        this IServiceCollection services,
        Action<DocumentRendererOptions>? configureOptions = null)
    {
        // Register options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register core services
        services.AddSingleton<IDocumentRenderer, DefaultDocumentRenderer>();
        services.AddSingleton<IDocumentLinkStore, InMemoryDocumentLinkStore>();
        services.AddSingleton<IDocumentRendererSelfTest, DocumentRendererSelfTest>();

        return services;
    }

    /// <summary>
    /// Adds Primus Document Renderer services with configuration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section to bind from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPrimusDocumentRenderer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DocumentRendererOptions>(configuration);

        // Register core services
        services.AddSingleton<IDocumentRenderer, DefaultDocumentRenderer>();
        services.AddSingleton<IDocumentLinkStore, InMemoryDocumentLinkStore>();
        services.AddSingleton<IDocumentRendererSelfTest, DocumentRendererSelfTest>();

        return services;
    }

    /// <summary>
    /// Adds Primus Document Renderer services with configuration section name.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The root configuration.</param>
    /// <param name="sectionName">The configuration section name (default: "PrimusDocuments").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPrimusDocumentRenderer(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "PrimusDocuments")
    {
        var section = configuration.GetSection(sectionName);
        return services.AddPrimusDocumentRenderer(section);
    }
}

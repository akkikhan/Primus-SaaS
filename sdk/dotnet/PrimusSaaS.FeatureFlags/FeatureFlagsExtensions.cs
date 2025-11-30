using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PrimusSaaS.FeatureFlags.Providers;

namespace PrimusSaaS.FeatureFlags;

/// <summary>
/// Extension methods for configuring PrimusSaaS Feature Flags services.
/// </summary>
public static class FeatureFlagsExtensions
{
    /// <summary>
    /// Adds PrimusSaaS Feature Flags services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">A delegate to configure the feature flags options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddPrimusFeatureFlags(options =>
    /// {
    ///     options.Provider = FeatureFlagProvider.InMemory;
    ///     options.Flags["NewDashboard"] = new FeatureFlagDefinition
    ///     {
    ///         Enabled = true,
    ///         RolloutPercentage = 50
    ///     };
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddPrimusFeatureFlags(
        this IServiceCollection services,
        Action<FeatureFlagsOptions> configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        // Configure options
        services.Configure(configure);

        // Register the appropriate provider based on configuration
        services.AddSingleton<IFeatureFlagProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FeatureFlagsOptions>>();
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<FeatureFlagsOptions>>();
            
            return options.Value.Provider switch
            {
                FeatureFlagProvider.JsonFile => ActivatorUtilities.CreateInstance<JsonFileFeatureFlagProvider>(sp),
                FeatureFlagProvider.AzureAppConfiguration => throw new NotSupportedException(
                    "Azure App Configuration provider requires the PrimusSaaS.FeatureFlags.AzureAppConfig package."),
                _ => new InMemoryFeatureFlagProvider(optionsMonitor)
            };
        });

        // Register the feature flag service
        services.TryAddSingleton<IFeatureFlagService, FeatureFlagService>();

        return services;
    }

    /// <summary>
    /// Adds PrimusSaaS Feature Flags services with configuration binding.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configurationSection">The configuration section name (default: "PrimusFeatureFlags").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // In Program.cs
    /// builder.Services.AddPrimusFeatureFlags("PrimusFeatureFlags");
    /// 
    /// // In appsettings.json
    /// {
    ///   "PrimusFeatureFlags": {
    ///     "Provider": "InMemory",
    ///     "Flags": {
    ///       "NewDashboard": { "Enabled": true }
    ///     }
    ///   }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddPrimusFeatureFlags(
        this IServiceCollection services,
        string configurationSection = "PrimusFeatureFlags")
    {
        return services.AddPrimusFeatureFlags(options => { });
    }
}

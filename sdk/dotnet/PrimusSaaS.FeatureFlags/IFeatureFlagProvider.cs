namespace PrimusSaaS.FeatureFlags;

/// <summary>
/// Provider interface for feature flag storage backends.
/// </summary>
public interface IFeatureFlagProvider
{
    /// <summary>
    /// Gets a feature flag definition by name.
    /// </summary>
    /// <param name="featureName">The name of the feature flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The feature flag definition, or null if not found.</returns>
    Task<FeatureFlagDefinition?> GetFlagAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature flag definitions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of all feature flags.</returns>
    Task<IReadOnlyDictionary<string, FeatureFlagDefinition>> GetAllFlagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the feature flag cache from the underlying storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}

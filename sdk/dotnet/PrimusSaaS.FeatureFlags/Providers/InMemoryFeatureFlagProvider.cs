using Microsoft.Extensions.Options;

namespace PrimusSaaS.FeatureFlags.Providers;

/// <summary>
/// In-memory feature flag provider using configuration-bound options.
/// </summary>
public class InMemoryFeatureFlagProvider : IFeatureFlagProvider
{
    private readonly IOptionsMonitor<FeatureFlagsOptions> _optionsMonitor;
    private Dictionary<string, FeatureFlagDefinition> _cache;
    private readonly object _lock = new();

    public InMemoryFeatureFlagProvider(IOptionsMonitor<FeatureFlagsOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        _cache = new Dictionary<string, FeatureFlagDefinition>(
            optionsMonitor.CurrentValue.Flags,
            StringComparer.OrdinalIgnoreCase);

        // Subscribe to configuration changes
        _optionsMonitor.OnChange(options =>
        {
            lock (_lock)
            {
                _cache = new Dictionary<string, FeatureFlagDefinition>(
                    options.Flags,
                    StringComparer.OrdinalIgnoreCase);
            }
        });
    }

    /// <inheritdoc />
    public Task<FeatureFlagDefinition?> GetFlagAsync(string featureName, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _cache.TryGetValue(featureName, out var definition);
            return Task.FromResult(definition);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, FeatureFlagDefinition>> GetAllFlagsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyDictionary<string, FeatureFlagDefinition>>(
                new Dictionary<string, FeatureFlagDefinition>(_cache, StringComparer.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc />
    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _cache = new Dictionary<string, FeatureFlagDefinition>(
                _optionsMonitor.CurrentValue.Flags,
                StringComparer.OrdinalIgnoreCase);
        }
        return Task.CompletedTask;
    }
}

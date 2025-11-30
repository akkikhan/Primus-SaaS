using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace PrimusSaaS.FeatureFlags.Providers;

/// <summary>
/// JSON file-based feature flag provider with hot-reload support.
/// </summary>
public class JsonFileFeatureFlagProvider : IFeatureFlagProvider, IDisposable
{
    private readonly FeatureFlagsOptions _options;
    private readonly ILogger<JsonFileFeatureFlagProvider> _logger;
    private readonly PhysicalFileProvider? _fileProvider;
    private readonly IDisposable? _changeToken;
    private Dictionary<string, FeatureFlagDefinition> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public JsonFileFeatureFlagProvider(
        IOptions<FeatureFlagsOptions> options,
        ILogger<JsonFileFeatureFlagProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.JsonFilePath))
        {
            throw new InvalidOperationException("JsonFilePath must be configured when using JsonFile provider.");
        }

        var fullPath = Path.GetFullPath(_options.JsonFilePath);
        var directory = Path.GetDirectoryName(fullPath) 
            ?? throw new InvalidOperationException($"Invalid file path: {fullPath}");
        var fileName = Path.GetFileName(fullPath);

        _fileProvider = new PhysicalFileProvider(directory);

        // Initial load
        LoadFromFile(fullPath);

        // Set up file watcher for hot-reload
        _changeToken = ChangeToken.OnChange(
            () => _fileProvider.Watch(fileName),
            () =>
            {
                _logger.LogInformation("Feature flags file changed, reloading...");
                LoadFromFile(fullPath);
            });
    }

    private void LoadFromFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("Feature flags file not found: {Path}", path);
                return;
            }

            var json = File.ReadAllText(path);
            var flags = JsonSerializer.Deserialize<Dictionary<string, FeatureFlagDefinition>>(json, JsonOptions);

            if (flags != null)
            {
                lock (_lock)
                {
                    _cache = new Dictionary<string, FeatureFlagDefinition>(flags, StringComparer.OrdinalIgnoreCase);
                }
                _logger.LogInformation("Loaded {Count} feature flags from {Path}", flags.Count, path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load feature flags from {Path}", path);
        }
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
        if (!string.IsNullOrWhiteSpace(_options.JsonFilePath))
        {
            LoadFromFile(Path.GetFullPath(_options.JsonFilePath));
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _changeToken?.Dispose();
        _fileProvider?.Dispose();
    }
}

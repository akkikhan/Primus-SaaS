namespace PrimusSaaS.FeatureFlags;

/// <summary>
/// Configuration options for PrimusSaaS Feature Flags.
/// </summary>
public class FeatureFlagsOptions
{
    /// <summary>
    /// The provider type to use for feature flag storage.
    /// Default is <see cref="FeatureFlagProvider.InMemory"/>.
    /// </summary>
    public FeatureFlagProvider Provider { get; set; } = FeatureFlagProvider.InMemory;

    /// <summary>
    /// Path to the JSON configuration file when using <see cref="FeatureFlagProvider.JsonFile"/>.
    /// </summary>
    public string? JsonFilePath { get; set; }

    /// <summary>
    /// Connection string for Azure App Configuration when using <see cref="FeatureFlagProvider.AzureAppConfiguration"/>.
    /// </summary>
    public string? AzureAppConfigConnectionString { get; set; }

    /// <summary>
    /// Azure App Configuration endpoint URL (alternative to connection string, uses DefaultAzureCredential).
    /// </summary>
    public string? AzureAppConfigEndpoint { get; set; }

    /// <summary>
    /// Label filter for Azure App Configuration feature flags.
    /// </summary>
    public string? AzureAppConfigLabel { get; set; }

    /// <summary>
    /// Cache duration for feature flag values in seconds.
    /// Default is 30 seconds.
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable real-time refresh for feature flags (where supported).
    /// Default is false.
    /// </summary>
    public bool EnableRealTimeRefresh { get; set; } = false;

    /// <summary>
    /// Refresh interval in seconds when real-time refresh is enabled.
    /// Default is 30 seconds.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Default value to return when a feature flag is not found.
    /// Default is false (disabled).
    /// </summary>
    public bool DefaultValue { get; set; } = false;

    /// <summary>
    /// In-memory feature flag definitions (used when Provider is InMemory).
    /// Key is the feature flag name, value is the flag configuration.
    /// </summary>
    public Dictionary<string, FeatureFlagDefinition> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Logging options for feature flag evaluation.
    /// </summary>
    public FeatureFlagLoggingOptions Logging { get; set; } = new();
}

/// <summary>
/// Provider type for feature flag storage.
/// </summary>
public enum FeatureFlagProvider
{
    /// <summary>
    /// In-memory storage using configuration binding.
    /// </summary>
    InMemory = 0,

    /// <summary>
    /// JSON file-based storage with hot-reload support.
    /// </summary>
    JsonFile = 1,

    /// <summary>
    /// Azure App Configuration with Microsoft.FeatureManagement integration.
    /// </summary>
    AzureAppConfiguration = 2
}

/// <summary>
/// Definition of a single feature flag.
/// </summary>
public class FeatureFlagDefinition
{
    /// <summary>
    /// Whether the feature flag is enabled globally.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Optional description of the feature flag.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Percentage rollout (0-100). If set, flag is enabled for this percentage of users.
    /// Uses consistent hashing based on user identifier.
    /// </summary>
    public int? RolloutPercentage { get; set; }

    /// <summary>
    /// List of user identifiers that should always have this flag enabled.
    /// </summary>
    public List<string> EnabledForUsers { get; set; } = new();

    /// <summary>
    /// List of group names that should always have this flag enabled.
    /// </summary>
    public List<string> EnabledForGroups { get; set; } = new();

    /// <summary>
    /// Start time for time-based activation (UTC).
    /// </summary>
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// End time for time-based activation (UTC).
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Custom metadata for the feature flag.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Logging options for feature flag evaluation.
/// </summary>
public class FeatureFlagLoggingOptions
{
    /// <summary>
    /// Whether to log feature flag evaluations.
    /// Default is true.
    /// </summary>
    public bool LogEvaluations { get; set; } = true;

    /// <summary>
    /// Whether to include user context in logs (may contain PII).
    /// Default is false.
    /// </summary>
    public bool IncludeUserContext { get; set; } = false;

    /// <summary>
    /// Minimum log level for evaluation logs.
    /// Default is Information.
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}

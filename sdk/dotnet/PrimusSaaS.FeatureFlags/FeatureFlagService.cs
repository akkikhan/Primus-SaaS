using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PrimusSaaS.FeatureFlags;

/// <summary>
/// Default implementation of <see cref="IFeatureFlagService"/> using in-memory configuration.
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly FeatureFlagsOptions _options;
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly IFeatureFlagProvider _provider;

    public FeatureFlagService(
        IOptions<FeatureFlagsOptions> options,
        ILogger<FeatureFlagService> logger,
        IFeatureFlagProvider provider)
    {
        _options = options.Value;
        _logger = logger;
        _provider = provider;
    }

    /// <inheritdoc />
    public bool IsEnabled(string featureName)
    {
        return IsEnabledAsync(featureName).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public bool IsEnabled(string featureName, string userId)
    {
        return IsEnabledAsync(featureName, userId).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public bool IsEnabled(string featureName, ClaimsPrincipal user)
    {
        return IsEnabledAsync(featureName, user).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public bool IsEnabled(string featureName, FeatureFlagContext context)
    {
        return IsEnabledAsync(featureName, context).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        return await IsEnabledAsync(featureName, new FeatureFlagContext(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureName, string userId, CancellationToken cancellationToken = default)
    {
        return await IsEnabledAsync(featureName, new FeatureFlagContext { UserId = userId }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureName, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        return await IsEnabledAsync(featureName, FeatureFlagContext.FromClaimsPrincipal(user), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureName, FeatureFlagContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(featureName))
        {
            throw new ArgumentException("Feature name cannot be null or empty.", nameof(featureName));
        }

        var definition = await _provider.GetFlagAsync(featureName, cancellationToken);
        
        if (definition == null)
        {
            LogEvaluation(featureName, _options.DefaultValue, "NotFound", context);
            return _options.DefaultValue;
        }

        var result = EvaluateFlag(featureName, definition, context);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, bool>> GetAllFlagsAsync(CancellationToken cancellationToken = default)
    {
        var flags = await _provider.GetAllFlagsAsync(cancellationToken);
        return flags.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Enabled,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<FeatureFlagDefinition?> GetFlagDefinitionAsync(string featureName, CancellationToken cancellationToken = default)
    {
        return await _provider.GetFlagAsync(featureName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _provider.RefreshAsync(cancellationToken);
        _logger.LogDebug("Feature flags refreshed from provider");
    }

    private bool EvaluateFlag(string featureName, FeatureFlagDefinition definition, FeatureFlagContext context)
    {
        // Check time-based activation
        if (definition.StartTime.HasValue && DateTimeOffset.UtcNow < definition.StartTime.Value)
        {
            LogEvaluation(featureName, false, "BeforeStartTime", context);
            return false;
        }

        if (definition.EndTime.HasValue && DateTimeOffset.UtcNow > definition.EndTime.Value)
        {
            LogEvaluation(featureName, false, "AfterEndTime", context);
            return false;
        }

        // Check user targeting
        if (!string.IsNullOrEmpty(context.UserId))
        {
            if (definition.EnabledForUsers.Contains(context.UserId, StringComparer.OrdinalIgnoreCase))
            {
                LogEvaluation(featureName, true, "UserTargeted", context);
                return true;
            }
        }

        // Check group targeting
        if (context.Groups.Count > 0)
        {
            foreach (var group in context.Groups)
            {
                if (definition.EnabledForGroups.Contains(group, StringComparer.OrdinalIgnoreCase))
                {
                    LogEvaluation(featureName, true, "GroupTargeted", context);
                    return true;
                }
            }
        }

        // Check percentage rollout
        if (definition.RolloutPercentage.HasValue && !string.IsNullOrEmpty(context.UserId))
        {
            var isInRollout = IsInRolloutPercentage(featureName, context.UserId, definition.RolloutPercentage.Value);
            LogEvaluation(featureName, isInRollout, $"Rollout_{definition.RolloutPercentage.Value}%", context);
            return isInRollout;
        }

        // Fall back to global enabled state
        LogEvaluation(featureName, definition.Enabled, "GlobalEnabled", context);
        return definition.Enabled;
    }

    private bool IsInRolloutPercentage(string featureName, string userId, int percentage)
    {
        if (percentage <= 0) return false;
        if (percentage >= 100) return true;

        // Use consistent hashing to ensure the same user always gets the same result
        var key = $"{featureName}:{userId}";
        var hash = GetConsistentHash(key);
        var bucket = Math.Abs(hash % 100);
        return bucket < percentage;
    }

    private static int GetConsistentHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToInt32(hash, 0);
    }

    private void LogEvaluation(string featureName, bool result, string reason, FeatureFlagContext context)
    {
        if (!_options.Logging.LogEvaluations) return;

        if (_options.Logging.IncludeUserContext)
        {
            _logger.LogInformation(
                "Feature flag '{FeatureName}' evaluated to {Result}. Reason: {Reason}. UserId: {UserId}",
                featureName, result, reason, context.UserId ?? "anonymous");
        }
        else
        {
            _logger.LogInformation(
                "Feature flag '{FeatureName}' evaluated to {Result}. Reason: {Reason}",
                featureName, result, reason);
        }
    }
}

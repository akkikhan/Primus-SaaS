using System.Security.Claims;

namespace PrimusSaaS.FeatureFlags;

/// <summary>
/// Service for evaluating feature flags.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks if a feature flag is enabled.
    /// </summary>
    /// <param name="featureName">The name of the feature flag.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    bool IsEnabled(string featureName);

    /// <summary>
    /// Checks if a feature flag is enabled for a specific user.
    /// </summary>
    /// <param name="featureName">The name of the feature flag.</param>
    /// <param name="userId">The user identifier for targeting.</param>
    /// <returns>True if the feature is enabled for the user, false otherwise.</returns>
    bool IsEnabled(string featureName, string userId);

    /// <summary>
    /// Checks if a feature flag is enabled for the current ClaimsPrincipal.
    /// </summary>
    /// <param name="featureName">The name of the feature flag.</param>
    /// <param name="user">The claims principal for targeting.</param>
    /// <returns>True if the feature is enabled for the user, false otherwise.</returns>
    bool IsEnabled(string featureName, ClaimsPrincipal user);

    /// <summary>
    /// Checks if a feature flag is enabled with custom context.
    /// </summary>
    /// <param name="featureName">The name of the feature flag.</param>
    /// <param name="context">Custom evaluation context.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    bool IsEnabled(string featureName, FeatureFlagContext context);

    /// <summary>
    /// Asynchronously checks if a feature flag is enabled.
    /// </summary>
    /// <param name="featureName">The name of the feature flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a feature flag is enabled for a specific user.
    /// </summary>
    /// <param name="featureName">The name of the feature flag.</param>
    /// <param name="userId">The user identifier for targeting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the feature is enabled for the user, false otherwise.</returns>
    Task<bool> IsEnabledAsync(string featureName, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a feature flag is enabled for the current ClaimsPrincipal.
    /// </summary>
    /// <param name="featureName">The name of the feature flag.</param>
    /// <param name="user">The claims principal for targeting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the feature is enabled for the user, false otherwise.</returns>
    Task<bool> IsEnabledAsync(string featureName, ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a feature flag is enabled with custom context.
    /// </summary>
    /// <param name="featureName">The name of the feature flag.</param>
    /// <param name="context">Custom evaluation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    Task<bool> IsEnabledAsync(string featureName, FeatureFlagContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available feature flags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of feature flag names and their current enabled state.</returns>
    Task<IReadOnlyDictionary<string, bool>> GetAllFlagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the definition of a specific feature flag.
    /// </summary>
    /// <param name="featureName">The name of the feature flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The feature flag definition, or null if not found.</returns>
    Task<FeatureFlagDefinition?> GetFlagDefinitionAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a refresh of feature flag values from the provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Context for feature flag evaluation with targeting.
/// </summary>
public class FeatureFlagContext
{
    /// <summary>
    /// User identifier for targeting.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User email for targeting.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Groups the user belongs to for targeting.
    /// </summary>
    public List<string> Groups { get; set; } = new();

    /// <summary>
    /// Custom attributes for targeting.
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();

    /// <summary>
    /// Creates a context from a ClaimsPrincipal.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns>Feature flag context populated from claims.</returns>
    public static FeatureFlagContext FromClaimsPrincipal(ClaimsPrincipal user)
    {
        var context = new FeatureFlagContext
        {
            UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                  ?? user.FindFirst("sub")?.Value,
            Email = user.FindFirst(ClaimTypes.Email)?.Value 
                 ?? user.FindFirst("email")?.Value
        };

        // Extract groups from common claim types
        var groupClaims = user.FindAll(ClaimTypes.Role)
            .Concat(user.FindAll("groups"))
            .Concat(user.FindAll("roles"))
            .Select(c => c.Value);

        context.Groups.AddRange(groupClaims);

        return context;
    }
}

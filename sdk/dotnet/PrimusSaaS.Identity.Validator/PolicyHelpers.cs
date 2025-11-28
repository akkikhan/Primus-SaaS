using Microsoft.AspNetCore.Authorization;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Helper for adding simple claim-based policies without hardcoding claim shapes.
/// </summary>
public static class PolicyHelpers
{
    /// <summary>
    /// Adds a policy that requires a claim with specific allowed values.
    /// </summary>
    public static void AddPrimusClaimPolicy(this AuthorizationOptions options, string policyName, string claimType, params string[] allowedValues)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(policyName)) throw new ArgumentException("Policy name is required.", nameof(policyName));
        if (string.IsNullOrWhiteSpace(claimType)) throw new ArgumentException("Claim type is required.", nameof(claimType));

        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAssertion(context =>
            {
                var values = context.User.FindAll(claimType).Select(c => c.Value);
                if (allowedValues == null || allowedValues.Length == 0)
                {
                    return values.Any(); // just requires presence
                }

                return values.Any(v => allowedValues.Contains(v, StringComparer.Ordinal));
            });
        });
    }

    /// <summary>
    /// Adds a policy that requires Auth0 permissions (all requiredPermissions must be present).
    /// </summary>
    public static void RequireAuth0Permissions(this AuthorizationOptions options, string policyName, params string[] requiredPermissions)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(policyName)) throw new ArgumentException("Policy name is required.", nameof(policyName));

        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAssertion(context =>
            {
                var permissions = context.User.FindAll(PrimusClaimTypes.Permission).Select(c => c.Value).ToList();

                if (requiredPermissions == null || requiredPermissions.Length == 0)
                {
                    return permissions.Any();
                }

                return requiredPermissions.All(req => permissions.Contains(req, StringComparer.Ordinal));
            });
        });
    }

    /// <summary>
    /// Adds a policy that requires ANY of the provided Auth0 permissions.
    /// </summary>
    public static void RequireAnyAuth0Permission(this AuthorizationOptions options, string policyName, params string[] acceptablePermissions)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(policyName)) throw new ArgumentException("Policy name is required.", nameof(policyName));

        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAssertion(context =>
            {
                var permissions = context.User.FindAll(PrimusClaimTypes.Permission).Select(c => c.Value).ToList();
                if (acceptablePermissions == null || acceptablePermissions.Length == 0)
                {
                    return permissions.Any();
                }

                return acceptablePermissions.Any(req => permissions.Contains(req, StringComparer.Ordinal));
            });
        });
    }
}

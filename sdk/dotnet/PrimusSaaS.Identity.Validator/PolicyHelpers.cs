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
}

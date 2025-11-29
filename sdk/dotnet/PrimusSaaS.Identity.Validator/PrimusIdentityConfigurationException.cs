namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Exception thrown when Primus Identity Validator configuration is invalid or incomplete.
/// Provides actionable error messages with links to documentation.
/// </summary>
public class PrimusIdentityConfigurationException : Exception
{
    /// <summary>
    /// The configuration property that caused the error.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// URL to relevant documentation for resolving the issue.
    /// </summary>
    public string? HelpUrl { get; }

    public PrimusIdentityConfigurationException(string message)
        : base(message)
    {
    }

    public PrimusIdentityConfigurationException(string message, string? propertyName, string? helpUrl = null)
        : base(message)
    {
        PropertyName = propertyName;
        HelpUrl = helpUrl;
    }

    public PrimusIdentityConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Validates Auth0 configuration and throws helpful exceptions with guidance.
/// </summary>
public static class Auth0ConfigurationValidator
{
    private const string Auth0SignupUrl = "https://auth0.com/signup";
    private const string Auth0DocsUrl = "https://auth0.com/docs/quickstart/backend/aspnet-core-webapi";

    /// <summary>
    /// Validates Auth0 configuration and throws <see cref="PrimusIdentityConfigurationException"/> with helpful messages.
    /// </summary>
    /// <param name="domain">Auth0 tenant domain</param>
    /// <param name="audience">Auth0 API audience/identifier</param>
    /// <exception cref="PrimusIdentityConfigurationException">Thrown when configuration is invalid</exception>
    public static void Validate(string? domain, string? audience)
    {
        ValidateDomain(domain);
        ValidateAudience(audience);
    }

    /// <summary>
    /// Validates the Auth0 domain configuration.
    /// </summary>
    public static void ValidateDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new PrimusIdentityConfigurationException(
                FormatError(
                    "Auth0 Domain is required but was null or empty",
                    "Check your configuration:",
                    "  - appsettings.json: \"Auth0:Domain\" should be set to your Auth0 tenant domain",
                    "  - Example: \"your-tenant.auth0.com\"",
                    "",
                    "Don't have Auth0 credentials?",
                    $"  - Sign up free at: {Auth0SignupUrl}",
                    $"  - Quick start guide: {Auth0DocsUrl}"),
                "Domain",
                Auth0SignupUrl);
        }

        var trimmed = domain.Trim().ToLowerInvariant();

        // Check for common mistakes
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            throw new PrimusIdentityConfigurationException(
                FormatError(
                    $"Auth0 Domain '{domain}' uses HTTP but HTTPS is required",
                    "Auth0 requires secure connections. Please use HTTPS or just the domain name:",
                    $"  - Current: \"{domain}\"",
                    $"  - Correct: \"{trimmed.Replace("http://", "").TrimEnd('/')}\""),
                "Domain",
                Auth0DocsUrl);
        }

        // Validate domain format
        var domainToCheck = trimmed
            .Replace("https://", "")
            .Replace("http://", "")
            .TrimEnd('/');

        if (!IsValidAuth0Domain(domainToCheck))
        {
            throw new PrimusIdentityConfigurationException(
                FormatError(
                    $"Auth0 Domain '{domain}' appears to be invalid",
                    "Expected format: 'your-tenant.auth0.com' or 'your-tenant.region.auth0.com'",
                    "",
                    "Common issues:",
                    "  - Missing '.auth0.com' suffix",
                    "  - Using API identifier instead of domain",
                    "  - Typo in tenant name",
                    "",
                    "Find your domain:",
                    "  1. Log in to Auth0 Dashboard",
                    "  2. Go to Settings → Tenant Settings",
                    "  3. Copy the 'Tenant Domain' value"),
                "Domain",
                "https://manage.auth0.com/dashboard");
        }
    }

    /// <summary>
    /// Validates the Auth0 audience configuration.
    /// </summary>
    public static void ValidateAudience(string? audience)
    {
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new PrimusIdentityConfigurationException(
                FormatError(
                    "Auth0 Audience is required but was null or empty",
                    "Check your configuration:",
                    "  - appsettings.json: \"Auth0:Audience\" should be set to your API identifier",
                    "  - Example: \"https://your-api\" or \"https://api.yourcompany.com\"",
                    "",
                    "Create an API in Auth0:",
                    "  1. Go to Auth0 Dashboard → Applications → APIs",
                    "  2. Click 'Create API'",
                    "  3. Set Name and Identifier (this becomes your Audience)",
                    "  4. Copy the Identifier value",
                    "",
                    $"Dashboard: https://manage.auth0.com/dashboard"),
                "Audience",
                "https://auth0.com/docs/get-started/apis");
        }
    }

    /// <summary>
    /// Validates organization configuration when organization validation is enabled.
    /// </summary>
    public static void ValidateOrganization(string? organizationId, bool isRequired)
    {
        if (isRequired && string.IsNullOrWhiteSpace(organizationId))
        {
            throw new PrimusIdentityConfigurationException(
                FormatError(
                    "Auth0 Organization ID is required but was not found in the token",
                    "Organization validation is enabled but the token doesn't contain an org_id claim.",
                    "",
                    "Possible causes:",
                    "  - User is not a member of an organization",
                    "  - Organization not passed during login flow",
                    "  - Token was issued before organization membership",
                    "",
                    "To fix:",
                    "  1. Ensure user is added to an organization in Auth0",
                    "  2. Pass organization ID during authentication",
                    "  3. Or set ValidateOrganization = false if not needed"),
                "org_id",
                "https://auth0.com/docs/manage-users/organizations");
        }
    }

    private static bool IsValidAuth0Domain(string domain)
    {
        // Valid patterns:
        // - tenant.auth0.com (standard)
        // - tenant.region.auth0.com (regional)
        // - tenant.custom.domain (custom domains - be lenient)

        if (string.IsNullOrWhiteSpace(domain))
            return false;

        // Must have at least one dot
        if (!domain.Contains('.'))
            return false;

        // Standard Auth0 domains
        if (domain.EndsWith(".auth0.com", StringComparison.OrdinalIgnoreCase))
            return true;

        // Regional Auth0 domains (e.g., tenant.eu.auth0.com)
        if (domain.Contains(".auth0.com", StringComparison.OrdinalIgnoreCase))
            return true;

        // Allow custom domains (enterprise feature) - just ensure it's a valid hostname format
        // Custom domains won't have auth0.com in them but are still valid
        return Uri.TryCreate($"https://{domain}", UriKind.Absolute, out _);
    }

    private static string FormatError(params string[] lines)
    {
        var header = "PrimusSaaS.Identity.Validator Configuration Error";
        var separator = new string('─', 50);
        
        return $"\n{separator}\n{header}\n{separator}\n\n" +
               string.Join("\n", lines) +
               $"\n\n{separator}\n";
    }
}

namespace PrimusSaaS.Identity.Validator.Migration;

/// <summary>
/// Helper to translate JwtBearer/Auth0-style settings into Primus Identity options.
/// </summary>
public static class JwtBearerMigrationHelper
{
    public class JwtBearerConfig
    {
        public string Authority { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string? RoleClaimName { get; set; }
    }

    /// <summary>
    /// Produces a configured <see cref="Auth0Options"/> from JwtBearer-style settings.
    /// </summary>
    public static Auth0Options ToAuth0Options(JwtBearerConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrWhiteSpace(config.Authority)) throw new ArgumentException("Authority is required.", nameof(config.Authority));
        if (string.IsNullOrWhiteSpace(config.Audience)) throw new ArgumentException("Audience is required.", nameof(config.Audience));

        var domain = config.Authority
            .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');

        var auth0 = new Auth0Options
        {
            Domain = domain
        };
        auth0.Audiences.Add(config.Audience);
        if (!string.IsNullOrWhiteSpace(config.RoleClaimName))
        {
            auth0.RoleClaimName = config.RoleClaimName;
        }

        return auth0;
    }
}

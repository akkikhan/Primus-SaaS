using Microsoft.Extensions.Configuration;

namespace PrimusSaaS.Portal.Api.Models;

public class JwtOptions
{
    public string? Key { get; set; }
    public string? Secret { get; set; } // Backwards compatibility with JwtSettings__Secret
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int ExpiryInMinutes { get; set; } = 60;

    public string? EffectiveKey => string.IsNullOrWhiteSpace(Key) ? Secret : Key;

    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        // Prefer the current Jwt section; fall back to legacy JwtSettings
        var jwtSection = configuration.GetSection("Jwt");
        var legacySection = configuration.GetSection("JwtSettings");

        var options = jwtSection.Get<JwtOptions>() ?? legacySection.Get<JwtOptions>() ?? new JwtOptions();

        // If legacy section provided Secret, copy into Key slot for consistency
        if (string.IsNullOrWhiteSpace(options.Key) && !string.IsNullOrWhiteSpace(options.Secret))
        {
            options.Key = options.Secret;
        }

        // Default issuer/audience if missing
        options.Issuer ??= "PrimusSaasPortal";
        options.Audience ??= "PrimusSaasPortalUsers";

        return options;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(EffectiveKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured. Set Jwt:Key or JwtSettings__Secret.");
        }

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("JWT issuer is not configured. Set Jwt:Issuer or JwtSettings__Issuer.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("JWT audience is not configured. Set Jwt:Audience or JwtSettings__Audience.");
        }
    }
}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Extension methods for configuring Primus SaaS Identity validation.
/// </summary>
public static class PrimusIdentityExtensions
{
    /// <summary>
    /// Adds Primus SaaS Identity validation to the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Primus Identity options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPrimusIdentity(
        this IServiceCollection services,
        Action<PrimusIdentityOptions> configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        // Configure options
        services.Configure(configure);

        // Validate options on startup
        services.AddSingleton<IValidateOptions<PrimusIdentityOptions>, PrimusIdentityOptionsValidator>();

        // Add authentication with JWT Bearer
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var sp = services.BuildServiceProvider();
                var primusOptions = sp.GetRequiredService<IOptions<PrimusIdentityOptions>>().Value;
                primusOptions.Validate();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = primusOptions.ValidateLifetime,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = primusOptions.Issuer,
                    ValidAudience = primusOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(primusOptions.JwtSecret)),
                    ClockSkew = primusOptions.ClockSkew
                };

                options.RequireHttpsMetadata = primusOptions.RequireHttpsMetadata;

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Primus Identity: Authentication failed - {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"Primus Identity: Token validated for user - {context.Principal?.Identity?.Name}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"Primus Identity: Authentication challenge - {context.Error}, {context.ErrorDescription}");
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}

/// <summary>
/// Validates Primus Identity options on startup.
/// </summary>
internal class PrimusIdentityOptionsValidator : IValidateOptions<PrimusIdentityOptions>
{
    public ValidateOptionsResult Validate(string? name, PrimusIdentityOptions options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (ArgumentException ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}

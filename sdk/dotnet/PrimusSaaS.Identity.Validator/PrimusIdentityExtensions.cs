using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Identity.Validator.Services;
using PrimusSaaS.Identity.Validator.Validators;
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

        // Register Azure AD services (used when Mode is AzureAd or Hybrid)
        services.AddHttpClient();
        services.AddSingleton<JwksCache>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PrimusIdentityOptions>>().Value;
            return new JwksCache(options.JwksCacheTtl);
        });
        services.AddSingleton<OpenIdConfigurationService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var options = sp.GetRequiredService<IOptions<PrimusIdentityOptions>>().Value;
            return new OpenIdConfigurationService(httpClient, options.JwksCacheTtl);
        });
        services.AddSingleton<JwksService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var cache = sp.GetRequiredService<JwksCache>();
            return new JwksService(httpClient, cache);
        });
        services.AddSingleton<AzureAdValidator>(sp =>
        {
            var configService = sp.GetRequiredService<OpenIdConfigurationService>();
            var jwksService = sp.GetRequiredService<JwksService>();
            return new AzureAdValidator(configService, jwksService);
        });

        // Add authentication with JWT Bearer
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var sp = services.BuildServiceProvider();
                var primusOptions = sp.GetRequiredService<IOptions<PrimusIdentityOptions>>().Value;
                primusOptions.Validate();

                // Configure based on validation mode
                if (primusOptions.Mode == ValidationMode.Local)
                {
                    ConfigureLocalMode(options, primusOptions);
                }
                else if (primusOptions.Mode == ValidationMode.AzureAd)
                {
                    ConfigureAzureAdMode(options, primusOptions, sp);
                }
                else if (primusOptions.Mode == ValidationMode.Hybrid)
                {
                    ConfigureHybridMode(options, primusOptions, sp);
                }

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

    /// <summary>
    /// Configures Local mode (symmetric key validation).
    /// </summary>
    private static void ConfigureLocalMode(JwtBearerOptions options, PrimusIdentityOptions primusOptions)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = primusOptions.ValidateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = primusOptions.Issuer,
            ValidAudience = primusOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(primusOptions.JwtSecret!)),
            ClockSkew = primusOptions.ClockSkew
        };
    }

    /// <summary>
    /// Configures Azure AD mode (asymmetric key validation with JWKS).
    /// </summary>
    private static void ConfigureAzureAdMode(
        JwtBearerOptions options,
        PrimusIdentityOptions primusOptions,
        IServiceProvider serviceProvider)
    {
        var validator = serviceProvider.GetRequiredService<AzureAdValidator>();

        // Use custom token validation
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // We'll validate manually in the event
            ValidateAudience = false, // We'll validate manually in the event
            ValidateLifetime = primusOptions.ValidateLifetime,
            ClockSkew = primusOptions.ClockSkew,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true
        };

        // Override token validation event
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                try
                {
                    var token = context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                    if (token == null)
                    {
                        context.Fail("Invalid token type.");
                        return;
                    }

                    // Validate using Azure AD validator
                    var principal = await validator.ValidateTokenAsync(
                        token.RawData,
                        primusOptions.TenantId!,
                        primusOptions.Audience ?? primusOptions.ClientId,
                        primusOptions.ValidateLifetime,
                        primusOptions.ClockSkew);

                    context.Principal = principal;
                    Console.WriteLine($"Primus Identity (Azure AD): Token validated for user - {principal.Identity?.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Primus Identity (Azure AD): Validation failed - {ex.Message}");
                    context.Fail(ex);
                }
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Primus Identity (Azure AD): Authentication failed - {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"Primus Identity (Azure AD): Authentication challenge - {context.Error}, {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    }

    /// <summary>
    /// Configures Hybrid mode (supports both Local and Azure AD validation).
    /// </summary>
    private static void ConfigureHybridMode(
        JwtBearerOptions options,
        PrimusIdentityOptions primusOptions,
        IServiceProvider serviceProvider)
    {
        var validator = serviceProvider.GetRequiredService<AzureAdValidator>();

        // Use custom token validation
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // We'll validate manually
            ValidateAudience = true,
            ValidAudience = primusOptions.Audience,
            ValidateLifetime = primusOptions.ValidateLifetime,
            ClockSkew = primusOptions.ClockSkew,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true
        };

        // Override token validation event to detect issuer
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                try
                {
                    var token = context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                    if (token == null)
                    {
                        context.Fail("Invalid token type.");
                        return;
                    }

                    // Check issuer to determine which validation to use
                    var issuer = token.Issuer;
                    var isAzureAdToken = issuer.Contains("login.microsoftonline.com") ||
                                        issuer.Contains("sts.windows.net");

                    if (isAzureAdToken)
                    {
                        // Validate as Azure AD token
                        var principal = await validator.ValidateTokenAsync(
                            token.RawData,
                            primusOptions.TenantId!,
                            primusOptions.Audience ?? primusOptions.ClientId,
                            primusOptions.ValidateLifetime,
                            primusOptions.ClockSkew);

                        context.Principal = principal;
                        Console.WriteLine($"Primus Identity (Hybrid - Azure AD): Token validated for user - {principal.Identity?.Name}");
                    }
                    else
                    {
                        // Validate as Local token (already validated by JWT Bearer middleware)
                        Console.WriteLine($"Primus Identity (Hybrid - Local): Token validated for user - {context.Principal?.Identity?.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Primus Identity (Hybrid): Validation failed - {ex.Message}");
                    context.Fail(ex);
                }
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Primus Identity (Hybrid): Authentication failed - {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"Primus Identity (Hybrid): Authentication challenge - {context.Error}, {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };

        // For Local tokens, add symmetric key
        options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(primusOptions.JwtSecret!));
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

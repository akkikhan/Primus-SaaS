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

        // Register services
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
                var azureValidator = sp.GetRequiredService<AzureAdValidator>();
                
                primusOptions.Validate();

                options.RequireHttpsMetadata = primusOptions.RequireHttpsMetadata;
                
                // Custom validation logic to handle multiple issuers
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false, // We'll validate manually based on config
                    ValidateAudience = false, // We'll validate manually based on config
                    ValidateLifetime = primusOptions.ValidateLifetime,
                    ClockSkew = primusOptions.ClockSkew,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true
                };

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

                            var issuer = token.Issuer;
                            var issuerConfig = primusOptions.Issuers.FirstOrDefault(i => i.Issuer == issuer);

                            if (issuerConfig == null)
                            {
                                context.Fail($"Untrusted issuer: {issuer}. No matching configuration found.");
                                return;
                            }

                            System.Security.Claims.ClaimsPrincipal principal;

                            if (issuerConfig.Type == IssuerType.Oidc)
                            {
                                // Validate as OIDC (Azure AD) token
                                if (string.IsNullOrEmpty(issuerConfig.Authority))
                                {
                                    context.Fail($"Authority URL is required for OIDC issuer {issuerConfig.Name}");
                                    return;
                                }

                                var tenantId = ExtractTenantId(issuerConfig.Authority);
                                if (string.IsNullOrEmpty(tenantId))
                                {
                                    context.Fail($"Could not extract Tenant ID from authority: {issuerConfig.Authority}");
                                    return;
                                }

                                // Use first audience for now
                                var audience = issuerConfig.Audiences.FirstOrDefault();

                                principal = await azureValidator.ValidateTokenAsync(
                                    token.RawData,
                                    tenantId,
                                    audience ?? string.Empty,
                                    primusOptions.ValidateLifetime,
                                    primusOptions.ClockSkew);
                            }
                            else // Jwt
                            {
                                // Validate as Local JWT token
                                if (string.IsNullOrEmpty(issuerConfig.Secret))
                                {
                                    context.Fail($"Shared secret is required for JWT issuer {issuerConfig.Name}");
                                    return;
                                }

                                var validationParameters = new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidIssuer = issuerConfig.Issuer,
                                    ValidateAudience = true,
                                    ValidAudiences = issuerConfig.Audiences,
                                    ValidateLifetime = primusOptions.ValidateLifetime,
                                    ValidateIssuerSigningKey = true,
                                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(issuerConfig.Secret)),
                                    ClockSkew = primusOptions.ClockSkew
                                };

                                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                                principal = handler.ValidateToken(token.RawData, validationParameters, out _);
                            }

                            context.Principal = principal;
                            Console.WriteLine($"Primus Identity: Token validated for user - {principal.Identity?.Name} (Issuer: {issuerConfig.Name})");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Primus Identity: Validation failed - {ex.Message}");
                            context.Fail(ex);
                        }
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Primus Identity: Authentication failed - {context.Exception.Message}");
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

    private static string? ExtractTenantId(string authority)
    {
        try
        {
            var uri = new Uri(authority);
            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // Handle https://login.microsoftonline.com/<tenant-id>/v2.0
            if (parts.Length >= 1)
            {
                // Check if first part is a GUID-like string
                if (Guid.TryParse(parts[0], out _))
                {
                    return parts[0];
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
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

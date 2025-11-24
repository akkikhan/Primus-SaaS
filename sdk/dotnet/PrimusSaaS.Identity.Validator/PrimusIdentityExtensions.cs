using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                var jwksService = sp.GetRequiredService<JwksService>();
                
                primusOptions.Validate();

                options.RequireHttpsMetadata = primusOptions.RequireHttpsMetadata;
                
                // Custom validation logic to handle multiple issuers
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, // Validate issuer against our config
                    ValidateAudience = true, // Validate audience against our config
                    ValidateLifetime = primusOptions.ValidateLifetime,
                    ClockSkew = primusOptions.ClockSkew,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    
                    // CRITICAL FIX: Resolve keys dynamically based on the token's issuer
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        var jwt = securityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                        if (jwt == null) return Enumerable.Empty<SecurityKey>();

                        var issuer = jwt.Issuer;
                        var issuerConfig = primusOptions.Issuers.FirstOrDefault(i => i.Issuer == issuer);

                        if (issuerConfig == null) return Enumerable.Empty<SecurityKey>();

                        if (issuerConfig.Type == IssuerType.Oidc)
                        {
                            // For OIDC, we rely on the AzureAdValidator/JwksService to have cached keys
                            // This is a synchronous call in a callback, which is tricky.
                            // Ideally, we pre-fetch or use a synchronous cache.
                            // For now, we'll use the JwksService synchronously (carefully) or rely on the fact 
                            // that AzureAdValidator handles this if we delegate.
                            
                            // BETTER APPROACH: Return empty here and let the custom validator handle it? 
                            // No, ValidateIssuerSigningKey=true will fail.
                            
                            // We need to fetch the keys.
                            var jwksService = sp.GetRequiredService<JwksService>();
                            
                            // If Authority is set, use it to find JWKS
                            var jwksUrl = !string.IsNullOrEmpty(issuerConfig.JwksUrl) 
                                ? issuerConfig.JwksUrl 
                                : $"{issuerConfig.Authority?.TrimEnd('/')}/discovery/v2.0/keys"; // Simplified guess, ideally use Discovery Doc
                                
                            // Sync-over-async is dangerous but required by this specific API surface
                            // In a real high-perf scenario, these should be background refreshed.
                            var keys = jwksService.GetJwksAsync(jwksUrl).GetAwaiter().GetResult();
                            
                            return keys.Keys.Select(k => 
                            {
                                var jsonKey = new Microsoft.IdentityModel.Tokens.JsonWebKey
                                {
                                    Kty = k.KeyType,
                                    Use = k.Use,
                                    Kid = k.KeyId,
                                    N = k.Modulus,
                                    E = k.Exponent,
                                    Alg = k.Algorithm,
                                    X5t = k.X509Thumbprint
                                };
                                if(k.X509CertificateChain != null) 
                                    foreach(var c in k.X509CertificateChain) jsonKey.X5c.Add(c);
                                return jsonKey;
                            });
                        }
                        else // JWT
                        {
                            if (!string.IsNullOrEmpty(issuerConfig.Secret))
                            {
                                return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(issuerConfig.Secret)) };
                            }
                        }

                        return Enumerable.Empty<SecurityKey>();
                    },
                    
                    // Validate Issuer matches one of our configs
                    IssuerValidator = (issuer, securityToken, validationParameters) => 
                    {
                        if (primusOptions.Issuers.Any(i => i.Issuer == issuer)) return issuer;
                        throw new SecurityTokenInvalidIssuerException($"Unknown issuer: {issuer}");
                    },

                    // Validate Audience matches the config for that issuer
                    AudienceValidator = (audiences, securityToken, validationParameters) =>
                    {
                        var jwt = securityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                        var issuerConfig = primusOptions.Issuers.FirstOrDefault(i => i.Issuer == jwt?.Issuer);
                        
                        if (issuerConfig == null) return false;
                        
                        // Ensure at least one token audience matches one configured audience
                        return audiences.Any(a => issuerConfig.Audiences.Contains(a));
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("PrimusSaaS.Identity.Validator");
                        logger?.LogWarning("Primus Identity: Authentication failed - {Reason}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("PrimusSaaS.Identity.Validator");
                        logger?.LogWarning("Primus Identity: Authentication challenge - {Error}, {ErrorDescription}", context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// Adds the Primus Identity Validator middleware to the pipeline.
    /// This enables authentication capabilities.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UsePrimusIdentityValidator(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        app.UseAuthentication();
        return app;
    }

    /// <summary>
    /// Adds the Tenant Isolation middleware to the pipeline.
    /// Ensures that all authenticated requests have a resolved TenantContext.
    /// </summary>
    public static IApplicationBuilder UsePrimusTenantIsolation(this IApplicationBuilder app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        return app.UseMiddleware<Middleware.TenantIsolationMiddleware>();
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

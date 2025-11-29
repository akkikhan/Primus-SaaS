using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Identity.Validator.Services;
using PrimusSaaS.Identity.Validator.Validators;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;

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
        services.AddSingleton<IdentityDiagnosticsService>();
        services.AddSingleton<SecurityEventMetrics>();
        services.AddSingleton<ISecurityEventLogger>(sp =>
        {
            var metrics = sp.GetRequiredService<SecurityEventMetrics>();
            var logger = sp.GetRequiredService<ILogger<DefaultSecurityEventLogger>>();
            var loggingOptions = sp.GetRequiredService<IOptions<PrimusIdentityOptions>>().Value.Logging;
            return new DefaultSecurityEventLogger(logger, metrics, loggingOptions);
        });
        services.AddSingleton(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<PrimusIdentityOptions>>().Value;
            return opt.RateLimiting;
        });
        services.AddSingleton<FailedValidationRateLimiter>();
        services.AddSingleton(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<PrimusIdentityOptions>>().Value;
            return opt.TokenRefresh;
        });
        services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
        services.AddSingleton<ITokenRefreshService>(sp =>
        {
            var options = sp.GetRequiredService<TokenRefreshOptions>();
            if (options.Enabled)
            {
                if (options.UseInMemoryStore || options.UseDurableStore)
                {
                    var store = sp.GetRequiredService<IRefreshTokenStore>();
                    return new DurableTokenRefreshService(options, store);
                }

                var customStore = sp.GetService<IRefreshTokenStore>();
                if (customStore != null)
                {
                    return new DurableTokenRefreshService(options, customStore);
                }
            }

            // If refresh is disabled, register a no-op stub to avoid nulls
            return new NoopTokenRefreshService();
        });

        // Add authentication with JWT Bearer
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var sp = services.BuildServiceProvider();
                var primusOptions = sp.GetRequiredService<IOptions<PrimusIdentityOptions>>().Value;

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
                        // Note: context is null here as TokenValidatedContext is not available in IssuerSigningKeyResolver
                        var issuerConfig = ResolveIssuerConfig(jwt, primusOptions, null);

                        if (issuerConfig == null) return Enumerable.Empty<SecurityKey>();

                        if (issuerConfig.Type.IsOidcBased())
                        {
                            // Resolve JWKS via discovery metadata to avoid malformed URLs (e.g., double /v2.0).
                            var jwksService = sp.GetRequiredService<JwksService>();
                            var configurationService = sp.GetRequiredService<OpenIdConfigurationService>();

                            var jwksUrl = issuerConfig.JwksUrl;

                            if (string.IsNullOrWhiteSpace(jwksUrl))
                            {
                                if (string.IsNullOrWhiteSpace(issuerConfig.Authority))
                                {
                                    throw new SecurityTokenInvalidIssuerException($"OIDC issuer '{issuerConfig.Name}' is missing Authority.");
                                }

                                var configuration = configurationService
                                    .GetConfigurationByAuthorityAsync(issuerConfig.Authority!)
                                    .GetAwaiter()
                                    .GetResult();

                                jwksUrl = configuration.JwksUri;
                            }

                            if (string.IsNullOrWhiteSpace(jwksUrl))
                            {
                                throw new SecurityTokenInvalidIssuerException($"OIDC issuer '{issuerConfig.Name}' did not expose a JWKS endpoint via discovery.");
                            }

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
                        // Note: context is null here as TokenValidatedContext is not available in AudienceValidator
                        var issuerConfig = ResolveIssuerConfig(jwt, primusOptions, null);
                        
                        if (issuerConfig == null) return false;
                        
                        // Ensure at least one token audience matches one configured audience
                        return audiences.Any(a => issuerConfig.Audiences.Contains(a, StringComparer.Ordinal));
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("PrimusSaaS.Identity.Validator");
                        var primusIdentityOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<PrimusIdentityOptions>>().Value;
                        var tenantResolver = ResolveTenantResolver(context.HttpContext, primusIdentityOptions);
                        var securityLogger = context.HttpContext.RequestServices.GetService<ISecurityEventLogger>();
                        var issuerConfig = primusIdentityOptions.Issuers.FirstOrDefault(i => i.Issuer == context.SecurityToken?.Issuer);

                        if (issuerConfig != null)
                        {
                            ApplyClaimMappings(context.Principal, issuerConfig, primusIdentityOptions.Logging, logger);
                            if (!EnsureOrganizationRequirement(context, issuerConfig))
                            {
                                return;
                            }
                            if (!EnsureMachineToMachineRequirement(context, issuerConfig))
                            {
                                return;
                            }
                            if (issuerConfig.RequireEmailVerification && !TokenClassification.IsMachineToMachine(context.Principal))
                            {
                                if (!TokenClassification.IsEmailVerified(context.Principal))
                                {
                                    context.Fail("Email verification is required.");
                                    return;
                                }
                            }

                            if (primusIdentityOptions.Logging.LogValidationSteps && logger != null && context.SecurityToken is JwtSecurityToken jwtToken)
                            {
                                var logData = IdentityLogHelper.BuildValidationLogData(jwtToken, context.Principal, primusIdentityOptions.Logging);
                                logger.Log(primusIdentityOptions.Logging.MinimumLevel, "Primus Identity: Token validated {@ValidationData}", logData);
                            }
                        }

                        if (tenantResolver == null)
                        {
                            securityLogger?.LogSuccessfulAuthentication(context.Principal!, context.SecurityToken?.Issuer);
                            return;
                        }

                        var tokenClaims = new TokenClaims(BuildClaimDictionary(context.Principal, context.SecurityToken));

                        try
                        {
                            var tenantContext = await tenantResolver.ResolveAsync(tokenClaims);

                            if (tenantContext != null)
                            {
                                context.HttpContext.Items["TenantContext"] = tenantContext;
                            }

                            securityLogger?.LogSuccessfulAuthentication(context.Principal!, context.SecurityToken?.Issuer);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Primus Identity: TenantResolver threw an exception. Authentication failed.");
                            context.Fail("Tenant resolution failed.");
                        }
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("PrimusSaaS.Identity.Validator");
                        logger?.LogWarning("Primus Identity: Authentication failed - {Reason}", context.Exception.Message);
                        var securityLogger = context.HttpContext.RequestServices.GetService<ISecurityEventLogger>();
                        securityLogger?.LogFailedAuthentication(null, context.Exception.Message);

                        var limiter = context.HttpContext.RequestServices.GetService<FailedValidationRateLimiter>();
                        if (limiter != null && limiter.RegisterFailure(context.HttpContext))
                        {
                            context.NoResult();
                            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                            context.Response.Headers["Retry-After"] = Math.Max(1, (int)primusOptions.ClockSkew.TotalSeconds).ToString();
                            logger?.LogWarning("Primus Identity: Rate limit hit for failed validations.");
                            securityLogger?.LogRateLimited(null, "Rate limit exceeded for failed validations.");
                        }
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

    private static ITenantResolver? ResolveTenantResolver(HttpContext httpContext, PrimusIdentityOptions options)
    {
        var resolverFromServices = httpContext.RequestServices.GetService<ITenantResolver>();
        if (resolverFromServices != null)
        {
            return resolverFromServices;
        }

        return options.TenantResolver != null
            ? new FuncTenantResolver(options.TenantResolver)
            : null;
    }

    private static IssuerConfig? ResolveIssuerConfig(System.IdentityModel.Tokens.Jwt.JwtSecurityToken? jwt, PrimusIdentityOptions options, TokenValidatedContext? context = null)
    {
        if (jwt == null) return null;

        // Multi-tenant Auth0 handling: try request-based resolver first
        if (options.Auth0MultiTenant?.Tenants.Count > 0)
        {
            var tenantKey = options.Auth0MultiTenant.ResolveTenant?.Invoke(context?.HttpContext!);
            if (!string.IsNullOrWhiteSpace(tenantKey) && options.Auth0MultiTenant.Tenants.TryGetValue(tenantKey!, out var auth0Options))
            {
                var cfg = auth0Options.ToIssuerConfig();
                return cfg.Issuer == jwt.Issuer ? cfg : null;
            }

            if (options.Auth0MultiTenant.ResolveFromIssuerWhenUnknown)
            {
                var match = options.Auth0MultiTenant.Tenants
                    .Select(kvp => kvp.Value.ToIssuerConfig())
                    .FirstOrDefault(c => c.Issuer == jwt.Issuer);
                if (match != null)
                {
                    return match;
                }
            }
        }

        return options.Issuers.FirstOrDefault(i => i.Issuer == jwt.Issuer);
    }

    private static Dictionary<string, object> BuildClaimDictionary(ClaimsPrincipal? principal, SecurityToken? securityToken)
    {
        var claimDictionary = new Dictionary<string, object>();

        void AddClaims(IEnumerable<Claim>? claimsToAdd)
        {
            if (claimsToAdd == null)
            {
                return;
            }

            foreach (var claim in claimsToAdd)
            {
                if (claimDictionary.TryGetValue(claim.Type, out var existing))
                {
                    if (existing is List<string> list)
                    {
                        list.Add(claim.Value);
                    }
                    else
                    {
                        claimDictionary[claim.Type] = new List<string>
                        {
                            existing?.ToString() ?? string.Empty,
                            claim.Value
                        };
                    }
                }
                else
                {
                    claimDictionary[claim.Type] = claim.Value;
                }
            }
        }

        if (securityToken is JwtSecurityToken jwt)
        {
            AddClaims(jwt.Claims);
        }

        AddClaims(principal?.Claims);

        return claimDictionary;
    }

    private static void ApplyClaimMappings(ClaimsPrincipal? principal, IssuerConfig issuerConfig, PrimusIdentityLoggingOptions loggingOptions, ILogger? logger)
    {
        if (principal?.Identity is not ClaimsIdentity identity || issuerConfig == null)
        {
            return;
        }

        foreach (var mapping in issuerConfig.ClaimMappings)
        {
            var sourceClaims = identity.FindAll(mapping.Key).ToList();
            foreach (var claim in sourceClaims)
            {
                if (!identity.HasClaim(c => c.Type == mapping.Value && c.Value == claim.Value))
                {
                    identity.AddClaim(new Claim(mapping.Value, claim.Value));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(issuerConfig.RoleClaimName))
        {
            CopyClaims(identity, issuerConfig.RoleClaimName!, ClaimTypes.Role);
        }

        var permissionType = string.IsNullOrWhiteSpace(issuerConfig.PermissionClaimName)
            ? PrimusClaimTypes.Permission
            : issuerConfig.PermissionClaimName;
        CopyClaims(identity, permissionType, PrimusClaimTypes.Permission);

        var organizationType = string.IsNullOrWhiteSpace(issuerConfig.OrganizationClaimName)
            ? PrimusClaimTypes.Organization
            : issuerConfig.OrganizationClaimName;
        CopyClaims(identity, organizationType, PrimusClaimTypes.Organization);

        if (loggingOptions.LogClaimMapping && logger != null)
        {
            logger.Log(loggingOptions.MinimumLevel, "Primus Identity: Claim mapping applied for issuer {Issuer}. Roles from {RoleClaim}, Permissions from {PermClaim}, Org from {OrgClaim}",
                issuerConfig.Issuer,
                issuerConfig.RoleClaimName ?? "(none)",
                permissionType,
                organizationType);
        }
    }

    private static void CopyClaims(ClaimsIdentity identity, string sourceType, string targetType)
    {
        if (string.IsNullOrWhiteSpace(sourceType) || string.IsNullOrWhiteSpace(targetType))
        {
            return;
        }

        var sourceClaims = identity.FindAll(sourceType).ToList();
        foreach (var claim in sourceClaims)
        {
            if (!identity.HasClaim(c => c.Type == targetType && c.Value == claim.Value))
            {
                identity.AddClaim(new Claim(targetType, claim.Value));
            }
        }
    }

    private static bool EnsureOrganizationRequirement(TokenValidatedContext context, IssuerConfig issuerConfig)
    {
        if (!issuerConfig.ValidateOrganization)
        {
            return true;
        }

        var orgClaimType = string.IsNullOrWhiteSpace(issuerConfig.OrganizationClaimName)
            ? PrimusClaimTypes.Organization
            : issuerConfig.OrganizationClaimName;

        var orgValues = context.Principal?.FindAll(orgClaimType).Select(c => c.Value).ToList() ?? new List<string>();
        if (!orgValues.Any())
        {
            context.Fail($"Organization claim '{orgClaimType}' is required.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(issuerConfig.RequiredOrganization) &&
            !orgValues.Contains(issuerConfig.RequiredOrganization, StringComparer.Ordinal))
        {
            context.Fail($"Organization '{issuerConfig.RequiredOrganization}' is required.");
            return false;
        }

        return true;
    }

    private static bool EnsureMachineToMachineRequirement(TokenValidatedContext context, IssuerConfig issuerConfig)
    {
        var isAllowed = TokenClassification.ValidateMachineToMachineAllowed(context.Principal, issuerConfig, out var error);
        if (!isAllowed)
        {
            context.Fail(error ?? "Machine-to-machine token not allowed.");
            return false;
        }
        return true;
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

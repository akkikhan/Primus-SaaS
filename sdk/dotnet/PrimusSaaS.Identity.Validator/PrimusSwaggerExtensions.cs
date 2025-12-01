using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Extensions for integrating Primus Identity with Swagger/OpenAPI.
/// </summary>
public static class PrimusSwaggerExtensions
{
    /// <summary>
    /// Configures Swagger to include security definitions for all configured Primus Identity issuers.
    /// Automatically adds the "Authorize" button to Swagger UI with the correct OAuth2/Bearer configuration.
    /// </summary>
    /// <param name="options">The Swagger generation options.</param>
    /// <param name="primusOptions">The Primus Identity options containing issuer configurations.</param>
    /// <returns>The Swagger generation options for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSwaggerGen(swagger =>
    /// {
    ///     swagger.AddPrimusIdentitySecurity(primusOptions);
    /// });
    /// </code>
    /// </example>
    public static SwaggerGenOptions AddPrimusIdentitySecurity(
        this SwaggerGenOptions options,
        PrimusIdentityOptions primusOptions)
    {
        if (primusOptions?.Issuers == null || primusOptions.Issuers.Count == 0)
        {
            // No issuers configured, add a generic Bearer token scheme
            AddBearerScheme(options, "Bearer", "JWT Bearer token authentication");
            return options;
        }

        foreach (var issuer in primusOptions.Issuers)
        {
            var schemeName = $"Primus-{issuer.Name}";
            
            if (issuer.Type.IsOidcBased())
            {
                AddOAuth2Scheme(options, schemeName, issuer);
            }
            else
            {
                AddBearerScheme(options, schemeName, $"JWT token from {issuer.Name}");
            }
        }

        return options;
    }

    /// <summary>
    /// Configures Swagger to include a simple Bearer token security definition for Primus Identity.
    /// Use this when you want a straightforward Bearer token input in Swagger UI.
    /// </summary>
    /// <param name="options">The Swagger generation options.</param>
    /// <param name="description">Optional description for the security scheme.</param>
    /// <returns>The Swagger generation options for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSwaggerGen(swagger =>
    /// {
    ///     swagger.AddPrimusIdentityBearerSecurity();
    /// });
    /// </code>
    /// </example>
    public static SwaggerGenOptions AddPrimusIdentityBearerSecurity(
        this SwaggerGenOptions options,
        string? description = null)
    {
        AddBearerScheme(options, "PrimusBearer", 
            description ?? "Enter your JWT token from any configured Primus Identity issuer (Auth0, Azure AD, etc.)");
        return options;
    }

    /// <summary>
    /// Configures Swagger with OAuth2 Authorization Code flow for Auth0.
    /// Enables interactive login through Swagger UI.
    /// </summary>
    /// <param name="options">The Swagger generation options.</param>
    /// <param name="domain">Auth0 domain (e.g., "my-tenant.auth0.com").</param>
    /// <param name="audience">API identifier/audience.</param>
    /// <param name="clientId">Auth0 application Client ID for Swagger UI.</param>
    /// <param name="scopes">Optional scopes to request. Defaults to "openid profile email".</param>
    /// <returns>The Swagger generation options for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSwaggerGen(swagger =>
    /// {
    ///     swagger.AddPrimusIdentityAuth0(
    ///         domain: "my-tenant.auth0.com",
    ///         audience: "https://my-api",
    ///         clientId: "swagger-client-id",
    ///         scopes: new[] { "openid", "profile", "read:data" });
    /// });
    /// </code>
    /// </example>
    public static SwaggerGenOptions AddPrimusIdentityAuth0(
        this SwaggerGenOptions options,
        string domain,
        string audience,
        string clientId,
        string[]? scopes = null)
    {
        var effectiveScopes = scopes ?? new[] { "openid", "profile", "email" };
        var scopeDict = effectiveScopes.ToDictionary(s => s, s => $"Access to {s}");

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Auth0 OAuth2 authentication. Click Authorize to login.",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"https://{domain}/authorize?audience={Uri.EscapeDataString(audience)}"),
                    TokenUrl = new Uri($"https://{domain}/oauth/token"),
                    Scopes = scopeDict
                }
            }
        };

        options.AddSecurityDefinition("PrimusAuth0", scheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "PrimusAuth0" }
                },
                effectiveScopes
            }
        });

        return options;
    }

    /// <summary>
    /// Configures Swagger with OAuth2 Authorization Code flow for Azure AD / Microsoft Entra ID.
    /// Enables interactive login through Swagger UI.
    /// </summary>
    /// <param name="options">The Swagger generation options.</param>
    /// <param name="tenantId">Azure AD tenant ID.</param>
    /// <param name="clientId">Azure AD application Client ID.</param>
    /// <param name="scopes">Optional scopes to request. Defaults to "openid profile email".</param>
    /// <returns>The Swagger generation options for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSwaggerGen(swagger =>
    /// {
    ///     swagger.AddPrimusIdentityAzureAD(
    ///         tenantId: "your-tenant-id",
    ///         clientId: "your-client-id",
    ///         scopes: new[] { "openid", "profile", "api://your-api/access" });
    /// });
    /// </code>
    /// </example>
    public static SwaggerGenOptions AddPrimusIdentityAzureAD(
        this SwaggerGenOptions options,
        string tenantId,
        string clientId,
        string[]? scopes = null)
    {
        var effectiveScopes = scopes ?? new[] { "openid", "profile", "email" };
        var scopeDict = effectiveScopes.ToDictionary(s => s, s => $"Access to {s}");

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Azure AD / Microsoft Entra ID authentication. Click Authorize to login.",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"),
                    TokenUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"),
                    Scopes = scopeDict
                }
            }
        };

        options.AddSecurityDefinition("PrimusAzureAD", scheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "PrimusAzureAD" }
                },
                effectiveScopes
            }
        });

        return options;
    }

    private static void AddBearerScheme(SwaggerGenOptions options, string schemeName, string description)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = description,
            In = ParameterLocation.Header,
            Name = "Authorization"
        };

        options.AddSecurityDefinition(schemeName, scheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = schemeName }
                },
                Array.Empty<string>()
            }
        });
    }

    private static void AddOAuth2Scheme(SwaggerGenOptions options, string schemeName, IssuerConfig issuer)
    {
        var scopes = new Dictionary<string, string>
        {
            { "openid", "OpenID Connect" },
            { "profile", "User profile" },
            { "email", "User email" }
        };

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = $"OAuth2 authentication via {issuer.Name}",
            Flows = new OpenApiOAuthFlows
            {
                // Use implicit flow as a fallback since we may not have full OAuth config
                Implicit = !string.IsNullOrEmpty(issuer.Authority) ? new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{issuer.Authority.TrimEnd('/')}/authorize"),
                    Scopes = scopes
                } : null
            }
        };

        options.AddSecurityDefinition(schemeName, scheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = schemeName }
                },
                scopes.Keys.ToArray()
            }
        });
    }
}

/// <summary>
/// Service collection extensions for Primus Identity Swagger integration.
/// </summary>
public static class PrimusSwaggerServiceExtensions
{
    /// <summary>
    /// Adds Swagger generation with Primus Identity security definitions automatically configured.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureSwagger">Optional callback to further configure Swagger.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Instead of:
    /// builder.Services.AddSwaggerGen();
    /// 
    /// // Use:
    /// builder.Services.AddSwaggerGenWithPrimusIdentity();
    /// </code>
    /// </example>
    public static IServiceCollection AddSwaggerGenWithPrimusIdentity(
        this IServiceCollection services,
        Action<SwaggerGenOptions>? configureSwagger = null)
    {
        services.AddSwaggerGen(options =>
        {
            // Add generic Bearer security by default
            options.AddPrimusIdentityBearerSecurity();
            
            // Apply any additional configuration
            configureSwagger?.Invoke(options);
        });

        return services;
    }
}

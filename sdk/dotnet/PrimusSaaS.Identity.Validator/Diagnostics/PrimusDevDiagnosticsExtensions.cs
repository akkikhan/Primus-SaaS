using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PrimusSaaS.Identity.Validator.Diagnostics;

/// <summary>
/// Extension methods for enhanced Primus Identity diagnostics in development environments.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide detailed diagnostic information to help debug authentication issues
/// during development. Features include:
/// </para>
/// <list type="bullet">
/// <item>Detailed WWW-Authenticate challenge headers</item>
/// <item>Token rejection logging with structured data</item>
/// <item>Diagnostic endpoints for viewing auth configuration and recent failures</item>
/// <item>Debug headers with failure context</item>
/// </list>
/// <para>
/// <strong>Security:</strong> These features are intended for development only. Use
/// <see cref="PrimusDiagnosticsOptions.ForProduction"/> in production environments.
/// </para>
/// </remarks>
public static class PrimusDevDiagnosticsExtensions
{
    /// <summary>
    /// Adds Primus Identity development diagnostics services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Auto-detect development environment
    /// builder.Services.AddPrimusDevDiagnostics();
    /// 
    /// // Or configure explicitly
    /// builder.Services.AddPrimusDevDiagnostics(options =>
    /// {
    ///     options.EnableDetailedErrors = true;
    ///     options.IncludeDebugHeaders = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddPrimusDevDiagnostics(
        this IServiceCollection services,
        Action<PrimusDiagnosticsOptions>? configure = null)
    {
        services.AddSingleton<PrimusDiagnosticsOptions>(sp =>
        {
            var options = new PrimusDiagnosticsOptions();
            configure?.Invoke(options);

            // Auto-detect development if enabled
            if (options.AutoDetectDevelopment)
            {
                var env = sp.GetService<IHostEnvironment>();
                if (env?.IsDevelopment() == true)
                {
                    options.EnableDetailedErrors = true;
                    options.IncludeTokenHintsInChallenges = true;
                    options.IncludeDebugHeaders = true;
                }
            }

            return options;
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<PrimusDiagnosticsOptions>();
            return new AuthFailureTracker(options.MaxRecentFailures);
        });

        return services;
    }

    /// <summary>
    /// Maps comprehensive Primus Identity diagnostics endpoints for development debugging.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="basePath">Base path for diagnostics endpoints. Default: "/_primus".</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    /// <remarks>
    /// <para>Exposes the following endpoints:</para>
    /// <list type="bullet">
    /// <item><c>GET {basePath}/diagnostics</c> - Overview of configured issuers and settings</item>
    /// <item><c>GET {basePath}/diagnostics/failures</c> - Recent authentication failures</item>
    /// <item><c>GET {basePath}/diagnostics/failures/stats</c> - Failure statistics by reason</item>
    /// <item><c>POST {basePath}/diagnostics/validate-token</c> - Test token validation</item>
    /// <item><c>DELETE {basePath}/diagnostics/failures</c> - Clear failure history</item>
    /// </list>
    /// <para>
    /// <strong>Warning:</strong> These endpoints expose configuration details. Only enable
    /// in development or behind strong authentication in staging environments.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// 
    /// if (app.Environment.IsDevelopment())
    /// {
    ///     app.MapPrimusDevDiagnostics();
    /// }
    /// </code>
    /// </example>
    public static IEndpointRouteBuilder MapPrimusDevDiagnostics(
        this IEndpointRouteBuilder endpoints,
        string basePath = "/_primus")
    {
        var diagnosticsPath = basePath + "/diagnostics";

        // Main diagnostics overview
        endpoints.MapGet(diagnosticsPath, GetDiagnosticsOverview);

        // Recent failures
        endpoints.MapGet(diagnosticsPath + "/failures", GetRecentFailures);

        // Failure stats
        endpoints.MapGet(diagnosticsPath + "/failures/stats", GetFailureStats);

        // Clear failures
        endpoints.MapDelete(diagnosticsPath + "/failures", ClearFailures);

        // Token validation test
        endpoints.MapPost(diagnosticsPath + "/validate-token", ValidateTokenTest);

        return endpoints;
    }

    private static IResult GetDiagnosticsOverview(
        IOptions<PrimusIdentityOptions>? identityOptions,
        PrimusDiagnosticsOptions? diagnosticsOptions,
        AuthFailureTracker? tracker,
        IHostEnvironment? env)
    {
        var options = identityOptions?.Value;

        var response = new
        {
            environment = env?.EnvironmentName ?? "Unknown",
            timestamp = DateTimeOffset.UtcNow,
            configuration = new
            {
                issuerCount = options?.Issuers?.Count ?? 0,
                issuers = options?.Issuers?.Select(i => new
                {
                    name = i.Name,
                    type = i.Type.ToString(),
                    issuer = MaskMiddle(i.Issuer),
                    audiences = i.Audiences.Select(a => MaskMiddle(a)).ToList(),
                    allowMachineToMachine = i.AllowMachineToMachine,
                    requireEmailVerification = i.RequireEmailVerification,
                    validateOrganization = i.ValidateOrganization
                }).ToList(),
                validateLifetime = options?.ValidateLifetime ?? true,
                requireHttpsMetadata = options?.RequireHttpsMetadata ?? true,
                clockSkewMinutes = options?.ClockSkew.TotalMinutes ?? 5
            },
            diagnostics = new
            {
                enableDetailedErrors = diagnosticsOptions?.EnableDetailedErrors ?? false,
                includeTokenHintsInChallenges = diagnosticsOptions?.IncludeTokenHintsInChallenges ?? false,
                includeDebugHeaders = diagnosticsOptions?.IncludeDebugHeaders ?? false,
                logTokenRejectionReasons = diagnosticsOptions?.LogTokenRejectionReasons ?? false,
                maxRecentFailures = diagnosticsOptions?.MaxRecentFailures ?? 0
            },
            failures = new
            {
                totalSinceStartup = tracker?.TotalFailures ?? 0,
                recentCount = tracker?.GetRecentFailures().Count ?? 0
            }
        };

        return Results.Ok(response);
    }

    private static IResult GetRecentFailures(
        AuthFailureTracker? tracker,
        int? limit,
        string? reason)
    {
        if (tracker == null)
        {
            return Results.Ok(new { message = "Failure tracking is not enabled.", failures = Array.Empty<object>() });
        }

        IReadOnlyList<AuthFailureEvent> failures;

        if (!string.IsNullOrEmpty(reason) && Enum.TryParse<AuthFailureReason>(reason, true, out var parsedReason))
        {
            failures = tracker.GetFailuresByReason(parsedReason);
        }
        else
        {
            failures = tracker.GetRecentFailures();
        }

        if (limit.HasValue && limit.Value > 0)
        {
            failures = failures.Take(limit.Value).ToList();
        }

        return Results.Ok(new
        {
            totalSinceStartup = tracker.TotalFailures,
            count = failures.Count,
            failures = failures.Select(f => new
            {
                timestamp = f.Timestamp,
                reason = f.Reason.ToString(),
                reasonDescription = AuthFailureClassifier.GetDescription(f.Reason),
                description = f.Description,
                tokenIssuer = f.TokenIssuer,
                tokenAudiences = f.TokenAudiences,
                requestPath = f.RequestPath,
                requestMethod = f.RequestMethod,
                correlationId = f.CorrelationId,
                configuredIssuers = f.ConfiguredIssuerNames
            })
        });
    }

    private static IResult GetFailureStats(AuthFailureTracker? tracker)
    {
        if (tracker == null)
        {
            return Results.Ok(new { message = "Failure tracking is not enabled.", stats = new Dictionary<string, int>() });
        }

        var stats = tracker.GetFailureStats();

        return Results.Ok(new
        {
            totalSinceStartup = tracker.TotalFailures,
            byReason = stats.Select(kvp => new
            {
                reason = kvp.Key.ToString(),
                description = AuthFailureClassifier.GetDescription(kvp.Key),
                count = kvp.Value
            }).OrderByDescending(x => x.count)
        });
    }

    private static IResult ClearFailures(AuthFailureTracker? tracker)
    {
        tracker?.Clear();
        return Results.Ok(new { message = "Failure history cleared." });
    }

    private static async Task<IResult> ValidateTokenTest(
        HttpContext context,
        IOptions<PrimusIdentityOptions>? identityOptions,
        ILoggerFactory? loggerFactory)
    {
        var logger = loggerFactory?.CreateLogger("PrimusSaaS.Identity.Diagnostics");
        var options = identityOptions?.Value;
        if (options == null)
        {
            return Results.BadRequest(new { error = "Primus Identity is not configured." });
        }

        string? token = null;

        // Try to get token from request body
        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            try
            {
                var json = JsonDocument.Parse(body);
                if (json.RootElement.TryGetProperty("token", out var tokenElement))
                {
                    token = tokenElement.GetString();
                }
            }
            catch
            {
                // Body might be the raw token
                token = body.Trim();
            }
        }

        // Try Authorization header
        if (string.IsNullOrEmpty(token))
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring(7);
            }
        }

        if (string.IsNullOrEmpty(token))
        {
            return Results.BadRequest(new
            {
                error = "No token provided.",
                hint = "Send token in request body as { \"token\": \"...\" } or in Authorization header."
            });
        }

        // Parse without validation to extract claims
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken? jwtToken = null;
        string? parseError = null;

        try
        {
            jwtToken = handler.ReadJwtToken(token);
        }
        catch (Exception ex)
        {
            parseError = ex.Message;
        }

        if (jwtToken == null)
        {
            return Results.BadRequest(new
            {
                error = "Failed to parse token.",
                details = parseError,
                reason = AuthFailureReason.MalformedToken.ToString()
            });
        }

        var tokenIssuer = jwtToken.Issuer;
        var tokenAudiences = jwtToken.Audiences.ToList();
        var matchedIssuer = options.Issuers.FirstOrDefault(i =>
            string.Equals(i.Issuer, tokenIssuer, StringComparison.Ordinal));

        var diagnosticResult = new
        {
            tokenInfo = new
            {
                issuer = tokenIssuer,
                audiences = tokenAudiences,
                algorithm = jwtToken.Header.Alg,
                keyId = jwtToken.Header.Kid,
                issuedAt = jwtToken.IssuedAt,
                validFrom = jwtToken.ValidFrom,
                validTo = jwtToken.ValidTo,
                isExpired = jwtToken.ValidTo < DateTime.UtcNow,
                expiresInSeconds = (jwtToken.ValidTo - DateTime.UtcNow).TotalSeconds,
                claims = jwtToken.Claims
                    .Where(c => !c.Type.Contains("token", StringComparison.OrdinalIgnoreCase))
                    .Select(c => new { type = c.Type, value = MaskSensitiveClaim(c.Type, c.Value) })
                    .ToList()
            },
            configurationMatch = new
            {
                issuerMatched = matchedIssuer != null,
                matchedIssuerName = matchedIssuer?.Name,
                audienceMatched = matchedIssuer != null &&
                    tokenAudiences.Any(a => matchedIssuer.Audiences.Contains(a, StringComparer.Ordinal)),
                configuredIssuers = options.Issuers.Select(i => new
                {
                    name = i.Name,
                    issuer = MaskMiddle(i.Issuer),
                    audiences = i.Audiences.Select(a => MaskMiddle(a)).ToList()
                }).ToList()
            },
            validation = new
            {
                wouldPass = matchedIssuer != null &&
                    tokenAudiences.Any(a => matchedIssuer.Audiences.Contains(a, StringComparer.Ordinal)) &&
                    jwtToken.ValidTo > DateTime.UtcNow &&
                    jwtToken.ValidFrom <= DateTime.UtcNow.Add(options.ClockSkew),
                issues = GetValidationIssues(jwtToken, matchedIssuer, options)
            }
        };

        return Results.Ok(diagnosticResult);
    }

    private static List<string> GetValidationIssues(
        JwtSecurityToken token,
        IssuerConfig? matchedIssuer,
        PrimusIdentityOptions options)
    {
        var issues = new List<string>();

        if (matchedIssuer == null)
        {
            issues.Add($"Issuer '{token.Issuer}' not found in configured issuers: [{string.Join(", ", options.Issuers.Select(i => i.Name))}]");
        }
        else
        {
            var tokenAudiences = token.Audiences.ToList();
            if (!tokenAudiences.Any(a => matchedIssuer.Audiences.Contains(a, StringComparer.Ordinal)))
            {
                issues.Add($"Token audiences [{string.Join(", ", tokenAudiences)}] don't match configured audiences [{string.Join(", ", matchedIssuer.Audiences)}]");
            }
        }

        if (token.ValidTo < DateTime.UtcNow)
        {
            issues.Add($"Token expired at {token.ValidTo:u} (expired {(DateTime.UtcNow - token.ValidTo).TotalMinutes:F1} minutes ago)");
        }

        if (token.ValidFrom > DateTime.UtcNow.Add(options.ClockSkew))
        {
            issues.Add($"Token not valid until {token.ValidFrom:u} (clock skew: {options.ClockSkew.TotalMinutes} minutes)");
        }

        return issues;
    }

    private static string MaskMiddle(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= 10)
            return value ?? "";

        var visibleChars = Math.Min(5, value.Length / 4);
        return value.Substring(0, visibleChars) + "..." + value.Substring(value.Length - visibleChars);
    }

    private static string MaskSensitiveClaim(string type, string value)
    {
        var sensitiveTypes = new[] { "sub", "email", "name", "phone", "address" };
        if (sensitiveTypes.Any(t => type.Contains(t, StringComparison.OrdinalIgnoreCase)))
        {
            return MaskMiddle(value);
        }
        return value;
    }
}

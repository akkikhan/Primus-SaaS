namespace PrimusSaaS.Identity.Validator.Diagnostics;

/// <summary>
/// Options for Primus Identity development-time diagnostics and debugging features.
/// </summary>
/// <remarks>
/// <para>
/// These options control the level of diagnostic information exposed by the SDK.
/// By default, enhanced diagnostics are enabled in Development environment and disabled in Production.
/// </para>
/// <para>
/// <strong>Security Note:</strong> Always disable enhanced diagnostics in production to avoid
/// exposing sensitive information about your authentication configuration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// builder.Services.AddPrimusIdentity(options =>
/// {
///     options.Diagnostics = new PrimusDiagnosticsOptions
///     {
///         EnableDetailedErrors = true,
///         IncludeTokenHintsInChallenges = true,
///         LogTokenRejectionReasons = true
///     };
/// });
/// </code>
/// </example>
public class PrimusDiagnosticsOptions
{
    /// <summary>
    /// When true, enables detailed error information in responses.
    /// Default: false (enabled via environment detection in Development).
    /// </summary>
    /// <remarks>
    /// When enabled:
    /// <list type="bullet">
    /// <item>WWW-Authenticate headers include detailed error_description</item>
    /// <item>X-Primus-Auth-Error headers contain structured failure information</item>
    /// <item>Diagnostic endpoints expose validation state</item>
    /// </list>
    /// <strong>Warning:</strong> Never enable in production.
    /// </remarks>
    public bool EnableDetailedErrors { get; set; }

    /// <summary>
    /// When true, WWW-Authenticate challenge headers include hints about why the token was rejected.
    /// Default: false.
    /// </summary>
    /// <remarks>
    /// Hints include:
    /// <list type="bullet">
    /// <item><c>error="invalid_token"</c> with <c>error_description</c> explaining the issue</item>
    /// <item>Information about expected vs. actual issuers</item>
    /// <item>Audience mismatch details</item>
    /// <item>Token expiration information</item>
    /// </list>
    /// </remarks>
    public bool IncludeTokenHintsInChallenges { get; set; }

    /// <summary>
    /// When true, logs detailed reasons for token rejection to the configured logger.
    /// Default: true in Development, false otherwise.
    /// </summary>
    /// <remarks>
    /// Logged information includes:
    /// <list type="bullet">
    /// <item>Token header metadata (alg, kid, typ)</item>
    /// <item>Issuer/audience validation results</item>
    /// <item>Signature verification failures</item>
    /// <item>Lifetime validation details</item>
    /// </list>
    /// <strong>Note:</strong> Token payloads are never logged, even with this enabled.
    /// </remarks>
    public bool LogTokenRejectionReasons { get; set; } = true;

    /// <summary>
    /// When true, adds an X-Primus-Auth-Debug header with structured diagnostic data on auth failures.
    /// Default: false.
    /// </summary>
    /// <remarks>
    /// The header contains a JSON-encoded object with:
    /// <list type="bullet">
    /// <item><c>reason</c> - The failure category (IssuerNotConfigured, AudienceMismatch, etc.)</item>
    /// <item><c>tokenIssuer</c> - The iss claim from the token (if parseable)</item>
    /// <item><c>tokenAudience</c> - The aud claim(s) from the token</item>
    /// <item><c>configuredIssuers</c> - List of configured issuer names (not full config)</item>
    /// <item><c>timestamp</c> - UTC timestamp of the failure</item>
    /// </list>
    /// </remarks>
    public bool IncludeDebugHeaders { get; set; }

    /// <summary>
    /// Maximum number of recent authentication failures to retain for diagnostics endpoint.
    /// Default: 50. Set to 0 to disable failure tracking.
    /// </summary>
    /// <remarks>
    /// Failures are stored in a circular buffer and exposed via the diagnostics endpoint.
    /// Only non-sensitive metadata is retained (no tokens or claims).
    /// </remarks>
    public int MaxRecentFailures { get; set; } = 50;

    /// <summary>
    /// Whether to automatically detect Development environment and enable diagnostics.
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// When enabled, the following options are automatically set in Development:
    /// <list type="bullet">
    /// <item><see cref="EnableDetailedErrors"/> = true</item>
    /// <item><see cref="IncludeTokenHintsInChallenges"/> = true</item>
    /// <item><see cref="IncludeDebugHeaders"/> = true</item>
    /// </list>
    /// </remarks>
    public bool AutoDetectDevelopment { get; set; } = true;

    /// <summary>
    /// Creates a new instance with default settings.
    /// </summary>
    public PrimusDiagnosticsOptions() { }

    /// <summary>
    /// Creates a development-optimized configuration with all diagnostics enabled.
    /// </summary>
    /// <returns>A configured <see cref="PrimusDiagnosticsOptions"/> for development use.</returns>
    public static PrimusDiagnosticsOptions ForDevelopment() => new()
    {
        EnableDetailedErrors = true,
        IncludeTokenHintsInChallenges = true,
        LogTokenRejectionReasons = true,
        IncludeDebugHeaders = true,
        MaxRecentFailures = 100
    };

    /// <summary>
    /// Creates a production-safe configuration with minimal diagnostics.
    /// </summary>
    /// <returns>A configured <see cref="PrimusDiagnosticsOptions"/> for production use.</returns>
    public static PrimusDiagnosticsOptions ForProduction() => new()
    {
        EnableDetailedErrors = false,
        IncludeTokenHintsInChallenges = false,
        LogTokenRejectionReasons = false,
        IncludeDebugHeaders = false,
        MaxRecentFailures = 0,
        AutoDetectDevelopment = false
    };
}

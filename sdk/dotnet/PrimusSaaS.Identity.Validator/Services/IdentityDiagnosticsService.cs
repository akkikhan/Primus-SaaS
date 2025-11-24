using Microsoft.Extensions.Options;

namespace PrimusSaaS.Identity.Validator.Services;

/// <summary>
/// Provides a diagnostics snapshot for Primus Identity (config + JWKS cache/fetch stats).
/// Intended for consumption by health/diagnostics endpoints.
/// </summary>
public class IdentityDiagnosticsService
{
    private readonly PrimusIdentityOptions _options;
    private readonly Func<JwksServiceDiagnostics> _jwksDiagnosticsProvider;
    private readonly SecurityEventMetrics _securityMetrics;

    public IdentityDiagnosticsService(
        IOptions<PrimusIdentityOptions> options,
        JwksService jwksService,
        SecurityEventMetrics securityMetrics)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _jwksDiagnosticsProvider = () => jwksService.GetDiagnostics();
        _securityMetrics = securityMetrics ?? throw new ArgumentNullException(nameof(securityMetrics));
    }

    /// <summary>
    /// For testing or custom wiring (allows injecting a diagnostics provider without a real JWKS service).
    /// </summary>
    public IdentityDiagnosticsService(
        PrimusIdentityOptions options,
        Func<JwksServiceDiagnostics> jwksDiagnosticsProvider,
        SecurityEventMetrics securityMetrics)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _jwksDiagnosticsProvider = jwksDiagnosticsProvider ?? throw new ArgumentNullException(nameof(jwksDiagnosticsProvider));
        _securityMetrics = securityMetrics ?? throw new ArgumentNullException(nameof(securityMetrics));
    }

    public IdentityDiagnosticsSnapshot GetSnapshot()
    {
        var securityMetrics = _options is { } ? _options : null;
        return new IdentityDiagnosticsSnapshot
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Issuers = ((_options?.Issuers) ?? new List<IssuerConfig>()).Select(i => new IssuerDiagnostics
            {
                Name = i.Name,
                Type = i.Type.ToString(),
                Authority = i.Authority,
                Issuer = i.Issuer,
                Audiences = i.Audiences?.ToArray() ?? Array.Empty<string>()
            }).ToList(),
            Jwks = _jwksDiagnosticsProvider.Invoke(),
            Security = _securityMetrics.Snapshot()
        };
    }
}

public class IdentityDiagnosticsSnapshot
{
    public DateTimeOffset GeneratedAtUtc { get; init; }
    public List<IssuerDiagnostics> Issuers { get; init; } = new();
    public JwksServiceDiagnostics Jwks { get; init; } = new();
    public SecurityEventMetricsSnapshot Security { get; init; } = new();
}

public class IssuerDiagnostics
{
    public string? Name { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? Authority { get; init; }
    public string Issuer { get; init; } = string.Empty;
    public string[] Audiences { get; init; } = Array.Empty<string>();
}

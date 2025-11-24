# Azure AD Troubleshooting (Primus Identity)

Claims mapping is intentionally out-of-scope for this guide (per current direction).

## Minimal Working Config
```csharp
services.AddPrimusIdentity(options =>
{
    options.Issuers = new List<IssuerConfig>
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.AzureAD,
            Authority = "https://login.microsoftonline.com/<tenant-id>",
            Issuer = "https://login.microsoftonline.com/<tenant-id>/v2.0",
            Audiences = new List<string> { "api://<client-id>" }
        }
    };
});
```

## Common Failures and Fixes
- **IDX10511 / JWKS 404**: Remove `/v2.0` from `Authority`; let discovery derive JWKS. Issuer should keep `/v2.0`.
- **Unknown issuer**: Ensure token `iss` matches configured `Issuer` exactly (case-sensitive).
- **Audience mismatch**: Ensure `aud` matches one configured audience.
- **Authority invalid**: Must be absolute HTTPS; validation now surfaces actionable errors.

## Diagnostics Endpoint
Expose issuer/JWKS/security metrics:
```csharp
app.MapPrimusIdentityDiagnostics(); // GET /primus/diagnostics
```
Snapshot includes:
- Issuers (name/type/authority/issuer/audiences)
- JWKS stats (cache hits/misses, fetch attempts/failures, last success)
- Security event metrics (auth successes/failures/rate-limited)

## JWKS Resiliency
- Configure retries/backoff via `JwksServiceOptions` (MaxRetries/BaseDelay).
- Monitor diagnostics for cache hits/misses and fetch failures; alert on sustained failures.

## Rate Limiting for Failed Auth
Enable to avoid abuse:
```csharp
options.RateLimiting = new FailedValidationRateLimiterOptions
{
    Enabled = true,
    MaxFailuresPerWindow = 20,
    MaxGlobalFailuresPerWindow = 0, // disable global limit
    Window = TimeSpan.FromMinutes(1)
};
```
Returns 429 with `Retry-After` when exceeded; security events are logged.

## Logging Recommendations
- Log authority/issuer and discovered JWKS URI at startup (INFO).
- Log JWKS retries at WARNING with attempt/delay.
- Avoid logging token contents; include issuer/audience and JWKS URL in failure logs.

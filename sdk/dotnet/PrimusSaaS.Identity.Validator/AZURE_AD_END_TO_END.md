# Azure AD End-to-End (Primus Identity Validator)

Claims mapping is excluded per current scope. This guide covers wiring, diagnostics, and troubleshooting.

## Prereqs
- Azure AD app registration with `Application ID URI` set (e.g., `api://<client-id>`).
- Tenant ID available.

## Backend Setup (Program.cs)
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new List<IssuerConfig>
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.AzureAD,
            Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}",
            Issuer = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0",
            Audiences = new List<string> { $"api://{builder.Configuration["AzureAd:ClientId"]}" }
        }
    };
    // Optional: rate limiting for failed validations
    options.RateLimiting = new FailedValidationRateLimiterOptions
    {
        Enabled = true,
        MaxFailuresPerWindow = 20,
        Window = TimeSpan.FromMinutes(1)
    };
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapPrimusIdentityDiagnostics(); // GET /primus/diagnostics

app.MapGet("/secure", () => "ok").RequireAuthorization();

app.Run();
```

## Frontend (MSAL)
- Use MSAL (@azure/msal-browser / @azure/msal-angular) to acquire tokens for the configured API scope (e.g., `api://<client-id>/.default` or custom scope).
- Send the access token as `Authorization: Bearer <token>` to the backend.

## Validation Checks
- `Authority`: no `/v2.0` in the authority; issuer includes `/v2.0`.
- `Audience`: matches token `aud`.
- Discovery: JWKS URI should be fetched from the metadata endpoint.

## Diagnostics
- Call `/primus/diagnostics` to view:
  - Issuers and audiences
  - JWKS cache hits/misses, fetch attempts/failures, last success
  - Security event metrics (auth successes/failures/rate-limited)

## Rate Limiting
- Configure `RateLimiting` to throttle repeated failed validations; 429 returned with `Retry-After`.

## Logging
- Log authority/issuer and discovered JWKS URI at startup.
- Log validation failures with issuer/audience and JWKS URL (avoid token contents).

## Common Pitfalls
- Double `/v2.0` in JWKS URL: remove `/v2.0` from `Authority`.
- Audience mismatch: token `aud` must match configured audience.
- Invalid authority: must be absolute HTTPS; errors now surfaced during validation.

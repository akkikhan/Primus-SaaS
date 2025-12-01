using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LiveDemoApi.Authentication;

/// <summary>
/// Fallback auth handler that returns a clear error when Primus Identity is commented out.
/// </summary>
public class IdentityDisabledAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "PrimusIdentityDisabled";

    public IdentityDisabledAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Always fail so downstream [Authorize] endpoints challenge with a clear message.
        return Task.FromResult(AuthenticateResult.Fail("Primus Identity Validator is disabled."));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        Response.ContentType = "application/json";

        var payload = new
        {
            error = "Primus Identity Validator is disabled in Program.cs.",
            hint = "Uncomment builder.Services.AddPrimusIdentity(...) and app.MapPrimusIdentityDiagnostics() to re-enable identity for the live demo."
        };

        return Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        HandleChallengeAsync(properties);
}

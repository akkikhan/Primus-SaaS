using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace PrimusSaaS.Identity.Validator.Tests.IntegrationHarness;

/// <summary>
/// Simplified harness to plug the fake auth handler into a test server.
/// </summary>
public static class IntegrationAuthBuilder
{
    public static IServiceCollection AddFakePrimusAuth(this IServiceCollection services)
    {
        services.AddAuthentication(FakePrimusAuthenticationHandler.Scheme)
            .AddScheme<AuthenticationSchemeOptions, FakePrimusAuthenticationHandler>(
                FakePrimusAuthenticationHandler.Scheme, _ => { });
        return services;
    }

    public static IApplicationBuilder UseFakePrimusAuth(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        return app;
    }
}

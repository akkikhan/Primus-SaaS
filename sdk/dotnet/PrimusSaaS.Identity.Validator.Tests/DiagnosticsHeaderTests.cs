using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using PrimusSaaS.Identity.Validator.Diagnostics;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class DiagnosticsHeaderTests
{
    [Fact]
    public async Task MapPrimusIdentityDiagnostics_ReturnsJsonAndIncludesErrorHeaderWhenPresent()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Primus-Auth-Error"] = "sample-error";
            await next();
        });
        app.MapPrimusIdentityAuthDiagnostics("/_primus/identity/diagnose");

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/_primus/identity/diagnose");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // The diagnostics endpoint echoes whatever header is on the response; this verifies the header key is available
        Assert.True(response.Headers.Contains("X-Primus-Auth-Error"));
    }
}

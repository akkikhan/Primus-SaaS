using FluentAssertions;
using Microsoft.AspNetCore.Http;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class Auth0MultiTenantOptionsTests
{
    [Fact]
    public void MultiTenantResolver_PicksTenantFromHost()
    {
        var options = new PrimusIdentityOptions
        {
            Auth0MultiTenant = new Auth0MultiTenantOptions
            {
                ResolveTenant = ctx => ctx.Request.Host.Host.Split('.').First()
            }
        };

        options.Auth0MultiTenant.Tenants["tenant-a"] = new Auth0Options
        {
            Domain = "tenant-a.auth0.com",
            Audiences = { "https://api-a" }
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("tenant-a.example.com");

        var cfg = options.Auth0MultiTenant.ResolveTenant!(httpContext);
        cfg.Should().Be("tenant-a");
    }
}

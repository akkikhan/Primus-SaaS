using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator.Tests;

public class Auth0PermissionAttributesTests
{
    private static AuthorizationFilterContext CreateContext(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void Auth0Permission_Forbid_WhenMissing()
    {
        var attr = new Auth0PermissionAttribute("read:clients");
        var ctx = CreateContext(new Claim(PrimusClaimTypes.Permission, "write:clients"));

        attr.OnAuthorization(ctx);

        ctx.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public void Auth0Permission_Allows_WhenPresent()
    {
        var attr = new Auth0PermissionAttribute("read:clients");
        var ctx = CreateContext(new Claim(PrimusClaimTypes.Permission, "read:clients"));

        attr.OnAuthorization(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public void Auth0Permissions_RequiresAll()
    {
        var attr = new Auth0PermissionsAttribute("read:clients", "write:clients");
        var ctx = CreateContext(
            new Claim(PrimusClaimTypes.Permission, "read:clients"),
            new Claim(PrimusClaimTypes.Permission, "write:clients"));

        attr.OnAuthorization(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public void Auth0Permissions_Forbid_WhenAnyMissing()
    {
        var attr = new Auth0PermissionsAttribute("read:clients", "write:clients");
        var ctx = CreateContext(new Claim(PrimusClaimTypes.Permission, "read:clients"));

        attr.OnAuthorization(ctx);

        ctx.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public void Auth0AnyPermission_Allows_WhenAnyMatch()
    {
        var attr = new Auth0AnyPermissionAttribute("read:clients", "read:all");
        var ctx = CreateContext(new Claim(PrimusClaimTypes.Permission, "read:clients"));

        attr.OnAuthorization(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public void Auth0AnyPermission_Forbid_WhenNoneMatch()
    {
        var attr = new Auth0AnyPermissionAttribute("read:clients", "read:all");
        var ctx = CreateContext(new Claim(PrimusClaimTypes.Permission, "write:clients"));

        attr.OnAuthorization(ctx);

        ctx.Result.Should().BeOfType<ForbidResult>();
    }
}

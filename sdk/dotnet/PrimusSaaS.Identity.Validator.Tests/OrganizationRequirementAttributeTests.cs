using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator.Tests;

public class OrganizationRequirementAttributeTests
{
    private static AuthorizationFilterContext CreateContext(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void RequireOrganization_Forbids_WhenMissing()
    {
        var attr = new RequireOrganizationAttribute();
        var ctx = CreateContext();

        attr.OnAuthorization(ctx);

        ctx.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public void RequireOrganization_Allows_WhenPresent()
    {
        var attr = new RequireOrganizationAttribute();
        var ctx = CreateContext(new Claim(PrimusClaimTypes.Organization, "org_123"));

        attr.OnAuthorization(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public void RequireOrganization_WithSpecificValue_ForbidsWhenNotMatching()
    {
        var attr = new RequireOrganizationAttribute("org_abc");
        var ctx = CreateContext(new Claim(PrimusClaimTypes.Organization, "org_other"));

        attr.OnAuthorization(ctx);

        ctx.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public void RequireOrganization_WithSpecificValue_AllowsWhenMatch()
    {
        var attr = new RequireOrganizationAttribute("org_abc");
        var ctx = CreateContext(new Claim(PrimusClaimTypes.Organization, "org_abc"));

        attr.OnAuthorization(ctx);

        ctx.Result.Should().BeNull();
    }
}

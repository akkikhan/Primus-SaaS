using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator.Tests;

public class PolicyHelpersTests
{
    [Fact]
    public void AddPrimusClaimPolicy_ShouldRequireClaim()
    {
        var options = new AuthorizationOptions();
        options.AddPrimusClaimPolicy("HasTid", "tid");

        options.GetPolicy("HasTid").Should().NotBeNull();
    }

    [Fact]
    public void AddPrimusClaimPolicy_ShouldAllowMatchingValue()
    {
        var options = new AuthorizationOptions();
        options.AddPrimusClaimPolicy("RequireRole", ClaimTypes.Role, "Admin");

        var policy = options.GetPolicy("RequireRole")!;
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "test"));
        var assertion = policy.Requirements.OfType<AssertionRequirement>().Single();
        var evalContext = new AuthorizationHandlerContext(policy.Requirements, user, null);
        var result = assertion.Handler(evalContext);

        result.Should().BeTrue();
    }

    [Fact]
    public void RequireAuth0Permissions_AllMustMatch()
    {
        var options = new AuthorizationOptions();
        options.RequireAuth0Permissions("AllPerms", "read:clients", "write:clients");

        var policy = options.GetPolicy("AllPerms")!;
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(PrimusClaimTypes.Permission, "read:clients"),
            new Claim(PrimusClaimTypes.Permission, "write:clients")
        }, "test"));

        var assertion = policy.Requirements.OfType<AssertionRequirement>().Single();
        var ctx = new AuthorizationHandlerContext(policy.Requirements, user, null);

        assertion.Handler(ctx).Should().BeTrue();
    }

    [Fact]
    public void RequireAnyAuth0Permission_AnyMatchSucceeds()
    {
        var options = new AuthorizationOptions();
        options.RequireAnyAuth0Permission("AnyPerm", "read:all", "read:clients");

        var policy = options.GetPolicy("AnyPerm")!;
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(PrimusClaimTypes.Permission, "read:clients")
        }, "test"));

        var assertion = policy.Requirements.OfType<AssertionRequirement>().Single();
        var ctx = new AuthorizationHandlerContext(policy.Requirements, user, null);

        assertion.Handler(ctx).Should().BeTrue();
    }
}

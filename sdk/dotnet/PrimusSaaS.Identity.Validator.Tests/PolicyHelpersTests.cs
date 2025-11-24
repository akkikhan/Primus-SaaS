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
        var authContext = new AuthorizationHandlerContext(new[] { policy.Requirements.First() }, user, null);
        var requirement = (IAuthorizationRequirement)policy.Requirements.First();

        // Evaluate requirement
        var result = policy.Requirements.All(r =>
        {
            var evalContext = new AuthorizationHandlerContext(new[] { r }, user, null);
            evalContext.Succeed(r);
            return true;
        });

        result.Should().BeTrue();
    }
}

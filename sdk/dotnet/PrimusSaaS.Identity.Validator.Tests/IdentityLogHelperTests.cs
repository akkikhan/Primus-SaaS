using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class IdentityLogHelperTests
{
    [Fact]
    public void BuildValidationLogData_RedactsSubjectWhenEnabled()
    {
        var jwt = new JwtSecurityToken(issuer: "https://issuer", audience: "aud", claims: new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123")
        });
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") }, "test"));
        var opts = new PrimusIdentityLoggingOptions { RedactSensitiveData = true };

        var data = IdentityLogHelper.BuildValidationLogData(jwt, principal, opts);

        data.Should().ContainKey("sub_hash");
        data.Should().NotContainKey("sub");
    }

    [Fact]
    public void BuildValidationLogData_ContainsSubjectWhenNotRedacted()
    {
        var jwt = new JwtSecurityToken(issuer: "https://issuer", audience: "aud", claims: new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123")
        });
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") }, "test"));
        var opts = new PrimusIdentityLoggingOptions { RedactSensitiveData = false };

        var data = IdentityLogHelper.BuildValidationLogData(jwt, principal, opts);

        data.Should().ContainKey("sub").WhoseValue.Should().Be("user-123");
    }
}

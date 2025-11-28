using FluentAssertions;
using PrimusSaaS.Identity.Validator.Migration;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class JwtBearerMigrationHelperTests
{
    [Fact]
    public void ToAuth0Options_MapsAuthorityAudienceAndRole()
    {
        var config = new JwtBearerMigrationHelper.JwtBearerConfig
        {
            Authority = "https://demo.auth0.com/",
            Audience = "https://api.demo",
            RoleClaimName = "https://api.demo/roles"
        };

        var opts = JwtBearerMigrationHelper.ToAuth0Options(config);

        opts.Domain.Should().Be("demo.auth0.com");
        opts.Audiences.Should().Contain("https://api.demo");
        opts.RoleClaimName.Should().Be("https://api.demo/roles");
    }
}

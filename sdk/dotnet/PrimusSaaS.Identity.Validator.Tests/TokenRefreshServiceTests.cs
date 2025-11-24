using FluentAssertions;
using PrimusSaaS.Identity.Validator.Services;

namespace PrimusSaaS.Identity.Validator.Tests;

public class TokenRefreshServiceTests
{
    [Fact]
    public async Task InMemoryService_Should_Issue_And_Refresh()
    {
        var options = new TokenRefreshOptions
        {
            Enabled = true,
            UseInMemoryStore = true,
            AccessTokenTtl = TimeSpan.FromMinutes(5),
            RefreshTokenTtl = TimeSpan.FromMinutes(30)
        };

        var service = new InMemoryTokenRefreshService(options);
        var refreshToken = await service.IssueRefreshTokenAsync("user-1");
        (await service.ValidateRefreshTokenAsync(refreshToken)).Should().BeTrue();

        var result = await service.RefreshAsync(refreshToken);
        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.NewRefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task NoopService_Should_Return_Failure()
    {
        var service = new NoopTokenRefreshService();
        var result = await service.RefreshAsync("token");
        result.Success.Should().BeFalse();
    }
}

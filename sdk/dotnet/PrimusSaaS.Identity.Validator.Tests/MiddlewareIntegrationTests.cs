using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Identity.Validator;
using Xunit;

namespace PrimusSaaS.Identity.Validator.Tests;

public class MiddlewareIntegrationTests
{
    private const string LocalSecret = "this-is-a-very-secure-secret-key-for-testing-purposes-only-32-chars";
    private const string LocalIssuer = "https://local.test";
    private const string LocalAudience = "api://test";

    [Fact]
    public async Task ValidToken_Should_Authenticate_And_Resolve_Tenant()
    {
        // Arrange
        using var server = CreateServer();
        var client = server.CreateClient();
        var token = GenerateToken(LocalIssuer, LocalAudience, LocalSecret, "tenant-1");

        // Act
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/secure");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("tenant-1");
    }

    [Fact]
    public async Task InvalidSecret_Should_Return_401()
    {
        // Arrange
        using var server = CreateServer();
        var client = server.CreateClient();
        // Sign with WRONG secret
        var token = GenerateToken(LocalIssuer, LocalAudience, "wrong-secret-key-wrong-secret-key-wrong-secret-key", "tenant-1");

        // Act
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/secure");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnknownIssuer_Should_Return_401()
    {
        // Arrange
        using var server = CreateServer();
        var client = server.CreateClient();
        // Sign with correct secret but WRONG issuer
        var token = GenerateToken("https://unknown.issuer", LocalAudience, LocalSecret, "tenant-1");

        // Act
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/secure");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MissingTenant_With_Isolation_Should_Return_403()
    {
        // Arrange
        using var server = CreateServer(enableIsolation: true);
        var client = server.CreateClient();
        // Token WITHOUT tenant claim
        var token = GenerateToken(LocalIssuer, LocalAudience, LocalSecret, tenantId: null);

        // Act
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/secure");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TenantResolver_Exception_Should_Return_401_Instead_Of_500()
    {
        // Arrange
        using var server = CreateServer(tenantResolverThrows: true);
        var client = server.CreateClient();
        var token = GenerateToken(LocalIssuer, LocalAudience, LocalSecret, "tenant-1");

        // Act
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/secure");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private TestServer CreateServer(bool enableIsolation = false, bool tenantResolverThrows = false)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAuthentication(); // Required for UseAuthentication
                services.AddAuthorization(); // Required for UseAuthorization
                
                services.AddPrimusIdentity(options =>
                {
                    options.Issuers = new List<IssuerConfig>
                    {
                        new IssuerConfig
                        {
                            Name = "Local",
                            Type = IssuerType.Jwt,
                            Issuer = LocalIssuer,
                            Audiences = new List<string> { LocalAudience },
                            Secret = LocalSecret
                        }
                    };

                    // Simple tenant resolver
                    options.TenantResolver = claims => 
                    {
                        if (tenantResolverThrows)
                        {
                            throw new InvalidOperationException("Resolver failure");
                        }

                        var tid = claims.Get("tid");
                        return tid == null ? null : new TenantContext { TenantId = tid };
                    };
                });
            })
            .Configure(app =>
            {
                app.UseAuthentication();
                
                // Enable isolation if requested
                if (enableIsolation)
                {
                    app.UsePrimusTenantIsolation();
                }

                app.UseRouting();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/api/secure", async context =>
                    {
                        var tenant = context.GetTenantContext();
                        await context.Response.WriteAsync($"Secure: {tenant?.TenantId}");
                    }).RequireAuthorization();
                });
            });

        return new TestServer(builder);
    }

    private string GenerateToken(string issuer, string audience, string secret, string? tenantId)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("sub", "user-1")
        };

        if (tenantId != null)
        {
            claims.Add(new Claim("tid", tenantId));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

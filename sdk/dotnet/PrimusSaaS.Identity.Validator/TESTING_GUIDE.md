# Testing Guide - .NET SDK

This guide covers how to test your **PrimusSaaS.Identity.Validator** integration, including verifying multi-issuer setups and using Postman.

## Table of Contents

1. [Verifying Multi-Issuer Configuration](#verifying-multi-issuer-configuration)
2. [Testing with Postman](#testing-with-postman)
3. [Integration Testing in .NET](#integration-testing-in-net)
4. [Troubleshooting Common Test Failures](#troubleshooting-common-test-failures)

---

## Verifying Multi-Issuer Configuration

When you have multiple issuers configured (e.g., Azure AD + LocalAuth), it's critical to verify which one is validating your token.

### 1. Inspect the `iss` Claim

The `iss` (Issuer) claim in the token tells you who issued it. The validator checks this against your configuration.

Create a debug endpoint to inspect the current user's identity:

```csharp
[HttpGet("whoami")]
[Authorize]
public IActionResult WhoAmI()
{
    var issuer = User.FindFirst("iss")?.Value;
    var subject = User.FindFirst("sub")?.Value;
    
    return Ok(new 
    { 
        message = $"Authenticated via {issuer}",
        issuer = issuer,
        subject = subject,
        claims = User.Claims.Select(c => new { c.Type, c.Value })
    });
}
```

### 2. Test Scenarios

| Scenario | Token Source | Expected Issuer (`iss`) | Result |
|----------|--------------|-------------------------|--------|
| **Scenario A** | Azure AD | `https://login.microsoftonline.com/...` | ✅ 200 OK |
| **Scenario B** | Local JWT Generator | `https://localhost:5265` | ✅ 200 OK |
| **Scenario C** | Unknown Issuer | `https://evil.com` | ❌ 401 Unauthorized |

---

## Testing with Postman

We provide a ready-to-use Postman collection to test your API.

### 1. Import Collection

Import `PrimusSaaS.Identity.Validator.postman_collection.json` into Postman.

### 2. Configure Variables

Set the following collection variables:

- `baseUrl`: Your API URL (e.g., `https://localhost:7001`)
- `localSecret`: Your configured local secret key
- `localIssuer`: Your configured local issuer URL

### 3. Generate Local Token (Pre-request Script)

The collection includes a Pre-request Script that automatically generates a valid JWT signed with your `localSecret`.

**To use it:**
1. Open the "Local Auth Request" folder.
2. Check the "Pre-request Script" tab.
3. It uses CryptoJS to sign a token matching your config.

### 4. Test Azure AD Token

1. Open "Azure AD Request".
2. Go to **Authorization** tab.
3. Type: **OAuth 2.0**.
4. Configure "Get New Access Token":
   - **Grant Type**: Authorization Code (or Client Credentials)
   - **Auth URL**: `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize`
   - **Token URL**: `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token`
   - **Client ID**: Your App ID
   - **Scope**: `api://{your-api-id}/.default`

---

## Integration Testing in .NET

You can write automated integration tests using `WebApplicationFactory`.

### 1. Setup Test Project

```bash
dotnet new xunit -n MyApi.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

### 2. Create Test Factory

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Optional: Override config for tests
            services.Configure<PrimusIdentityOptions>(options =>
            {
                options.Issuers = new List<IssuerConfig>
                {
                    new IssuerConfig 
                    {
                        Name = "TestAuth",
                        Type = IssuerType.Jwt,
                        Issuer = "https://test.local",
                        Secret = "test-secret-key-must-be-32-chars-long",
                        Audiences = new[] { "api://test" }
                    }
                };
            });
        });
    }
}
```

### 3. Write Test

```csharp
public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSecureData_WithValidToken_ReturnsOk()
    {
        // Arrange
        var token = GenerateTestToken(); // Helper to generate JWT
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/secure");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
```

---

## Troubleshooting Common Test Failures

### ❌ 401 Unauthorized

- **Check Logs**: Look at server console/logs.
- **Audience**: Does your token's `aud` match the config?
- **Issuer**: Does your token's `iss` match the config?
- **HTTPS**: Are you testing on HTTP but `RequireHttpsMetadata` is true?

### ❌ 403 Forbidden

- **Authentication Succeeded**, but **Authorization Failed**.
- Check `[Authorize(Roles = "...")]` attributes.
- Verify your token has the required `roles` claim.

### ❌ "Kid" not found (Azure AD)

- The signing key rolled over or is invalid.
- Ensure your `Authority` URL is correct in config.

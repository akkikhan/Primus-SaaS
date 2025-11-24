# Token Generation Guide - .NET SDK

This guide shows you how to generate JWT tokens that the **PrimusSaaS.Identity.Validator** package will successfully validate.

> [!IMPORTANT]
> **Critical Requirement**: The `Secret`, `Issuer`, and `Audience` values used when generating tokens **MUST EXACTLY MATCH** your validator configuration. Mismatches will cause validation failures.

## Table of Contents

1. [Local JWT Token Generation](#local-jwt-token-generation)
2. [Azure AD Token Acquisition](#azure-ad-token-acquisition)
3. [Critical Configuration Matching](#critical-configuration-matching)
4. [Common Mistakes](#common-mistakes)
5. [Testing Your Tokens](#testing-your-tokens)

---

## Local JWT Token Generation

### Prerequisites

Install the JWT library:

```bash
dotnet add package System.IdentityModel.Tokens.Jwt
```

### Complete Working Example

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class TokenGenerator
{
    public static string GenerateLocalJwtToken(string userId, string email, string name)
    {
        // ⚠️ CRITICAL: These values MUST match your PrimusIdentity configuration
        var secret = "your-super-secret-key-at-least-32-characters-long!";
        var issuer = "https://localhost:5265";
        var audience = "api://32979413-dcc7-4efa-b8b2-47a7208be405";
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", userId),
                new Claim("email", email),
                new Claim("name", name),
                new Claim("aud", audience),
                new Claim("iss", issuer),
                // Optional: Add roles for authorization
                new Claim("roles", "User"),
                new Claim("roles", "Admin")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

### Using in an Auth Controller

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    
    public AuthController(IConfiguration config)
    {
        _config = config;
    }
    
    [HttpPost("local-login")]
    public IActionResult LocalLogin([FromBody] LoginRequest request)
    {
        // TODO: Validate credentials against your user database
        if (!ValidateCredentials(request.Email, request.Password))
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }
        
        // ⚠️ CRITICAL: Load these from configuration to ensure they match
        var secret = _config["PrimusIdentity:Issuers:1:Secret"];
        var issuer = _config["PrimusIdentity:Issuers:1:Issuer"];
        var audience = _config["PrimusIdentity:Issuers:1:Audiences:0"];
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", request.Email),
                new Claim("email", request.Email),
                new Claim("name", request.Email.Split('@')[0]),
                new Claim("aud", audience),
                new Claim("iss", issuer)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Ok(new { token = tokenHandler.WriteToken(token) });
    }
    
    private bool ValidateCredentials(string email, string password)
    {
        // Implement your credential validation logic
        return true; // Placeholder
    }
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
```

---

## Azure AD Token Acquisition

For Azure AD OIDC tokens, you don't generate them yourself - users authenticate with Microsoft and receive tokens.

### Client-Side (MSAL.NET)

```csharp
using Microsoft.Identity.Client;

public class AzureAdTokenService
{
    private readonly IPublicClientApplication _app;
    
    public AzureAdTokenService()
    {
        _app = PublicClientApplicationBuilder
            .Create("YOUR_CLIENT_ID")
            .WithAuthority("https://login.microsoftonline.com/YOUR_TENANT_ID")
            .WithRedirectUri("http://localhost")
            .Build();
    }
    
    public async Task<string> AcquireTokenAsync()
    {
        var scopes = new[] { "api://YOUR_API_ID/.default" };
        
        try
        {
            // Try silent acquisition first
            var accounts = await _app.GetAccountsAsync();
            var result = await _app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                .ExecuteAsync();
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            // Interactive login required
            var result = await _app.AcquireTokenInteractive(scopes)
                .ExecuteAsync();
            return result.AccessToken;
        }
    }
}
```

### Server-Side (Confidential Client)

```csharp
using Microsoft.Identity.Client;

public class AzureAdServerTokenService
{
    private readonly IConfidentialClientApplication _app;
    
    public AzureAdServerTokenService(IConfiguration config)
    {
        _app = ConfidentialClientApplicationBuilder
            .Create(config["AzureAd:ClientId"])
            .WithClientSecret(config["AzureAd:ClientSecret"])
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{config["AzureAd:TenantId"]}"))
            .Build();
    }
    
    public async Task<string> AcquireTokenForApiAsync()
    {
        var scopes = new[] { "api://YOUR_API_ID/.default" };
        var result = await _app.AcquireTokenForClient(scopes).ExecuteAsync();
        return result.AccessToken;
    }
}
```

---

## Critical Configuration Matching

> [!CAUTION]
> **Token validation will fail if these values don't match exactly between generation and validation.**

### Your Validator Configuration (appsettings.json)

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "LocalAuth",
        "Type": "Jwt",
        "Issuer": "https://localhost:5265",
        "Secret": "your-super-secret-key-at-least-32-characters-long!",
        "Audiences": [ "api://32979413-dcc7-4efa-b8b2-47a7208be405" ]
      }
    ]
  }
}
```

### Your Token Generation Code MUST Use

| Configuration Value | Token Generation Value | Where Used |
|---------------------|------------------------|------------|
| `Issuers[0]:Secret` | `key` in `SigningCredentials` | Signature generation |
| `Issuers[0]:Issuer` | `tokenDescriptor.Issuer` | `iss` claim |
| `Issuers[0]:Audiences[0]` | `tokenDescriptor.Audience` | `aud` claim |

### Best Practice: Load from Configuration

```csharp
// ✅ CORRECT: Load from same config source
var secret = _config["PrimusIdentity:Issuers:1:Secret"];
var issuer = _config["PrimusIdentity:Issuers:1:Issuer"];
var audience = _config["PrimusIdentity:Issuers:1:Audiences:0"];

// ❌ WRONG: Hardcoded values that might not match
var secret = "different-secret";
var issuer = "LocalAuth"; // Should be full URL!
var audience = "http://localhost:5265"; // Wrong format!
```

---

## Common Mistakes

### ❌ Mistake #1: Secret Key Mismatch

```csharp
// Token Generation
var secret = "secret-key-123";

// Validator Configuration
"Secret": "different-secret-key-456"  // ❌ Won't validate!
```

**Error**: `Invalid signature`  
**Fix**: Use the exact same secret in both places.

---

### ❌ Mistake #2: Issuer Format Incorrect

```csharp
// Token Generation
Issuer = "LocalAuth"  // ❌ Wrong! This is a name, not a URL

// Validator Configuration
"Issuer": "https://localhost:5265"  // Expects full URL
```

**Error**: `Untrusted issuer: LocalAuth`  
**Fix**: Use full URL format: `https://localhost:5265`

---

### ❌ Mistake #3: Audience Mismatch

```csharp
// Token Generation
Audience = "http://localhost:5265"  // ❌ Wrong format

// Validator Configuration
"Audiences": [ "api://32979413-dcc7-4efa-b8b2-47a7208be405" ]
```

**Error**: `Invalid audience`  
**Fix**: Use the API identifier format: `api://your-app-id`

---

### ❌ Mistake #4: Missing Required Claims

```csharp
// ❌ Missing 'sub' claim
Subject = new ClaimsIdentity(new[]
{
    new Claim("email", "user@example.com"),
    new Claim("name", "John Doe")
    // Missing: new Claim("sub", userId)
})
```

**Error**: `GetPrimusUser()` returns null or incomplete data  
**Fix**: Always include `sub`, `email`, and `name` claims.

---

## Testing Your Tokens

### 1. Decode Token at jwt.io

Visit [https://jwt.io](https://jwt.io) and paste your token to verify:

- **Header**: Should show `"alg": "HS256"` for Local JWT
- **Payload**: Check `iss`, `aud`, `sub`, `exp` claims
- **Signature**: Paste your secret to verify signature is valid

### 2. Test Against Your API

```csharp
using System.Net.Http.Headers;

var httpClient = new HttpClient();
var token = GenerateLocalJwtToken("user123", "user@example.com", "John Doe");

httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

var response = await httpClient.GetAsync("https://localhost:5265/api/secure");

if (response.IsSuccessStatusCode)
{
    Console.WriteLine("✅ Token validated successfully!");
    var data = await response.Content.ReadAsStringAsync();
    Console.WriteLine(data);
}
else
{
    Console.WriteLine($"❌ Validation failed: {response.StatusCode}");
    var error = await response.Content.ReadAsStringAsync();
    Console.WriteLine(error);
}
```

### 3. Check Server Logs

Enable detailed authentication logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication": "Debug",
      "PrimusSaaS.Identity.Validator": "Debug"
    }
  }
}
```

---

## Quick Reference Checklist

Before deploying your token generation code:

- [ ] Secret key matches between generation and validation
- [ ] Issuer is full URL format (e.g., `https://localhost:5265`)
- [ ] Audience uses API identifier format (e.g., `api://your-app-id`)
- [ ] All required claims included: `sub`, `email`, `name`, `aud`, `iss`
- [ ] Token expiration set appropriately (`Expires`)
- [ ] Signature algorithm is `HmacSha256Signature` for Local JWT
- [ ] Configuration values loaded from same source (appsettings.json)
- [ ] Tested token at jwt.io to verify structure
- [ ] Tested against actual API endpoint

---

## Next Steps

- See [ERROR_REFERENCE.md](./ERROR_REFERENCE.md) for detailed troubleshooting
- See [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md) for secret management in production
- See [README.md](./README.md) for validator configuration examples

---

**Need Help?**
- GitHub Issues: https://github.com/akkikhan/Primus-SaaS/issues
- Documentation: https://docs.primus-saas.com

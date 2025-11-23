# Error Reference Guide - .NET SDK

Complete troubleshooting guide for **PrimusSaaS.Identity.Validator** validation errors.

## Table of Contents

1. [Invalid Signature](#invalid-signature)
2. [Invalid Issuer / Untrusted Issuer](#invalid-issuer--untrusted-issuer)
3. [Invalid Audience](#invalid-audience)
4. [Token Expired](#token-expired)
5. [Missing Required Configuration](#missing-required-configuration)
6. [JWKS Fetch Failures](#jwks-fetch-failures)
7. [Debugging Tips](#debugging-tips)

---

## Invalid Signature

### Error Message
```
IDX10503: Signature validation failed. Keys tried: '[PII is hidden]'
```
or
```
Invalid signature
```

### Causes

#### 1. Secret Key Mismatch (Local JWT)

**Problem**: The secret used to sign the token doesn't match the validator's configured secret.

```csharp
// Token Generation
var secret = "secret-key-123";

// Validator Configuration
"Secret": "different-secret-456"  // ❌ Mismatch!
```

**Solution**: Ensure both use the exact same secret.

```csharp
// ✅ Load from same configuration source
var secret = _config["PrimusIdentity:Issuers:1:Secret"];

// Token generation
var key = Encoding.UTF8.GetBytes(secret);
var signingCredentials = new SigningCredentials(
    new SymmetricSecurityKey(key),
    SecurityAlgorithms.HmacSha256Signature
);
```

#### 2. Wrong Signing Algorithm

**Problem**: Token signed with different algorithm than expected.

```csharp
// ❌ Token signed with RS256, validator expects HS256
SigningCredentials = new SigningCredentials(
    rsaKey,
    SecurityAlgorithms.RsaSha256  // Wrong for Local JWT!
)
```

**Solution**: Use `HmacSha256Signature` for Local JWT with shared secret.

```csharp
// ✅ Correct for Local JWT
SigningCredentials = new SigningCredentials(
    new SymmetricSecurityKey(key),
    SecurityAlgorithms.HmacSha256Signature
)
```

#### 3. JWKS Key ID Mismatch (Azure AD)

**Problem**: Token's `kid` (Key ID) not found in JWKS.

**Solution**: 
- Verify Azure AD configuration is correct
- Check JWKS cache hasn't expired
- Ensure `Authority` URL is correct

---

## Invalid Issuer / Untrusted Issuer

### Error Message
```
Untrusted issuer: LocalAuth
```
or
```
IDX10205: Issuer validation failed. Issuer: 'LocalAuth'. Did not match: validationParameters.ValidIssuer: 'https://localhost:5265'
```

### Causes

#### 1. Issuer Format Incorrect

**Problem**: Using friendly name instead of full URL.

```csharp
// Token Generation
Issuer = "LocalAuth"  // ❌ Wrong! This is a name, not a URL

// Validator Configuration
"Issuer": "https://localhost:5265"  // Expects full URL
```

**Solution**: Use full URL format in token generation.

```csharp
// ✅ Correct
Issuer = "https://localhost:5265"
```

#### 2. Issuer Not Configured

**Problem**: Token's `iss` claim doesn't match any configured issuer.

```json
// Token has: "iss": "https://auth.example.com"

// But configuration only has:
{
  "Issuers": [
    {
      "Issuer": "https://localhost:5265"  // ❌ Doesn't match!
    }
  ]
}
```

**Solution**: Add the issuer to your configuration.

```json
{
  "Issuers": [
    {
      "Name": "ExampleAuth",
      "Type": "Jwt",
      "Issuer": "https://auth.example.com",  // ✅ Matches token
      "Secret": "...",
      "Audiences": ["..."]
    }
  ]
}
```

#### 3. Case Sensitivity

**Problem**: Issuer URLs are case-sensitive.

```csharp
// Token: "iss": "https://localhost:5265"
// Config: "Issuer": "https://LocalHost:5265"  // ❌ Case mismatch!
```

**Solution**: Ensure exact case match.

---

## Invalid Audience

### Error Message
```
IDX10214: Audience validation failed. Audiences: 'http://localhost:5265'. Did not match: validationParameters.ValidAudience: 'api://32979413-dcc7-4efa-b8b2-47a7208be405'
```

### Causes

#### 1. Audience Format Mismatch

**Problem**: Using URL instead of API identifier format.

```csharp
// Token Generation
Audience = "http://localhost:5265"  // ❌ Wrong format

// Validator Configuration
"Audiences": [ "api://32979413-dcc7-4efa-b8b2-47a7208be405" ]
```

**Solution**: Use API identifier format.

```csharp
// ✅ Correct
Audience = "api://32979413-dcc7-4efa-b8b2-47a7208be405"
```

#### 2. Audience Not in Allowed List

**Problem**: Token's `aud` claim not in configured audiences array.

```json
// Token has: "aud": "api://app-123"

// But configuration only allows:
{
  "Audiences": [ "api://app-456" ]  // ❌ Doesn't match!
}
```

**Solution**: Add the audience to the allowed list.

```json
{
  "Audiences": [
    "api://app-123",  // ✅ Now allowed
    "api://app-456"
  ]
}
```

---

## Token Expired

### Error Message
```
IDX10223: Lifetime validation failed. The token is expired.
```

### Causes

#### 1. Token Actually Expired

**Problem**: Token's `exp` (expiration) claim is in the past.

**Solution**: Generate a new token or increase expiration time.

```csharp
// ✅ Set appropriate expiration
Expires = DateTime.UtcNow.AddHours(1)  // 1 hour from now
```

#### 2. Clock Skew Issues

**Problem**: Server clocks are out of sync.

**Solution**: Increase `ClockSkew` tolerance.

```json
{
  "PrimusIdentity": {
    "ClockSkew": "00:10:00"  // 10 minutes tolerance
  }
}
```

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.ClockSkew = TimeSpan.FromMinutes(10);
});
```

#### 3. Using Local Time Instead of UTC

**Problem**: Token expiration set using local time.

```csharp
// ❌ Wrong: Uses local time
Expires = DateTime.Now.AddHours(1)

// ✅ Correct: Uses UTC
Expires = DateTime.UtcNow.AddHours(1)
```

---

## Missing Required Configuration

### Error Message
```
Authority URL required for OIDC issuer
```

### Cause

OIDC issuer missing `Authority` property.

```json
{
  "Name": "AzureAD",
  "Type": "Oidc",
  "Issuer": "https://login.microsoftonline.com/TENANT/v2.0"
  // ❌ Missing: "Authority"
}
```

### Solution

Add `Authority` URL for OIDC issuers.

```json
{
  "Name": "AzureAD",
  "Type": "Oidc",
  "Issuer": "https://login.microsoftonline.com/TENANT/v2.0",
  "Authority": "https://login.microsoftonline.com/TENANT/v2.0",  // ✅ Added
  "Audiences": ["api://your-app-id"]
}
```

---

### Error Message
```
Shared secret required for JWT issuer
```

### Cause

JWT issuer missing `Secret` property.

```json
{
  "Name": "LocalAuth",
  "Type": "Jwt",
  "Issuer": "https://localhost:5265"
  // ❌ Missing: "Secret"
}
```

### Solution

Provide shared secret for JWT issuers.

```json
{
  "Name": "LocalAuth",
  "Type": "Jwt",
  "Issuer": "https://localhost:5265",
  "Secret": "your-secret-key-min-32-chars",  // ✅ Added
  "Audiences": ["api://your-app-id"]
}
```

---

## JWKS Fetch Failures

### Error Message
```
Failed to retrieve JWKS from authority
```

### Causes

#### 1. Network Issues

**Problem**: Cannot reach JWKS endpoint.

**Solution**: 
- Check network connectivity
- Verify firewall rules
- Ensure DNS resolution works

#### 2. Invalid Authority URL

**Problem**: Authority URL is incorrect.

```json
{
  "Authority": "https://login.microsoftonline.com/WRONG_TENANT/v2.0"
}
```

**Solution**: Verify tenant ID is correct.

#### 3. HTTPS Metadata Requirement

**Problem**: Using HTTP in production with `RequireHttpsMetadata: true`.

**Solution**: 
- For development: Set `RequireHttpsMetadata: false`
- For production: Use HTTPS

```json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": false  // Development only!
  }
}
```

---

## Debugging Tips

### 1. Enable Detailed Logging

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

### 2. Decode Token at jwt.io

Visit [https://jwt.io](https://jwt.io) and paste your token to inspect:
- Header (algorithm, key ID)
- Payload (iss, aud, sub, exp claims)
- Signature validity

### 3. Check Token Claims

```csharp
[HttpGet("debug")]
[Authorize]
public IActionResult DebugToken()
{
    var claims = User.Claims.Select(c => new { c.Type, c.Value });
    return Ok(claims);
}
```

### 4. Validate Configuration Match

Create a debug endpoint to verify configuration:

```csharp
[HttpGet("config-debug")]
public IActionResult ConfigDebug()
{
    return Ok(new
    {
        issuer = _config["PrimusIdentity:Issuers:1:Issuer"],
        audience = _config["PrimusIdentity:Issuers:1:Audiences:0"],
        secretLength = _config["PrimusIdentity:Issuers:1:Secret"]?.Length
    });
}
```

### 5. Test Token Generation Separately

```csharp
// Generate token
var token = GenerateToken();

// Immediately try to validate it
var tokenHandler = new JwtSecurityTokenHandler();
var validationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = issuer,
    ValidateAudience = true,
    ValidAudience = audience,
    ValidateLifetime = true,
    IssuerSigningKey = new SymmetricSecurityKey(key)
};

try
{
    var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
    Console.WriteLine("✅ Token is valid");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Validation failed: {ex.Message}");
}
```

---

## Quick Diagnostic Checklist

When you encounter a validation error:

- [ ] Decode token at jwt.io to inspect claims
- [ ] Verify `iss` claim matches configured `Issuer` exactly
- [ ] Verify `aud` claim is in configured `Audiences` array
- [ ] Verify `exp` claim is in the future (UTC)
- [ ] Check secret key matches (for Local JWT)
- [ ] Check algorithm is `HS256` (for Local JWT) or `RS256` (for Azure AD)
- [ ] Enable debug logging to see detailed error messages
- [ ] Verify configuration is loaded correctly (check appsettings.json)
- [ ] Test token generation and validation in isolation

---

## Need More Help?

- See [TOKEN_GENERATION_GUIDE.md](./TOKEN_GENERATION_GUIDE.md) for token generation examples
- See [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md) for production best practices
- GitHub Issues: https://github.com/akkikhan/Primus-SaaS/issues
- Documentation: https://portal.primus-saas.com/docs

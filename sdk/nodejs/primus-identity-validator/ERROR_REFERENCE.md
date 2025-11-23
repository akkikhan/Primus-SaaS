# Error Reference Guide - Node.js SDK

Complete troubleshooting guide for **primus-identity-validator** validation errors.

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
invalid signature
```
or
```
JsonWebTokenError: invalid signature
```

### Causes

#### 1. Secret Key Mismatch (Local JWT)

**Problem**: The secret used to sign the token doesn't match the validator's configured secret.

```javascript
// Token Generation
const secret = 'secret-key-123';

// Validator Configuration
secret: 'different-secret-456'  // ❌ Mismatch!
```

**Solution**: Ensure both use the exact same secret.

```javascript
// ✅ Load from same environment variable
const JWT_SECRET = process.env.JWT_SECRET;

// Token generation
const token = jwt.sign(payload, JWT_SECRET, options);

// Validator configuration
const primusAuth = primusIdentityMiddleware({
  issuers: [{
    secret: JWT_SECRET  // ✅ Same source
  }]
});
```

#### 2. Wrong Signing Algorithm

**Problem**: Token signed with different algorithm than expected.

```javascript
// ❌ Token signed with RS256, validator expects HS256
const options = {
  algorithm: 'RS256'  // Wrong for shared secret!
};
```

**Solution**: Use `HS256` for Local JWT with shared secret.

```javascript
// ✅ Correct for Local JWT
const options = {
  algorithm: 'HS256'
};
```

#### 3. JWKS Key ID Mismatch (Azure AD)

**Problem**: Token's `kid` (Key ID) not found in JWKS.

**Solution**: 
- Verify Azure AD configuration is correct
- Check JWKS cache hasn't expired
- Ensure `authority` URL is correct

---

## Invalid Issuer / Untrusted Issuer

### Error Message
```
Untrusted issuer: LocalAuth
```
or
```
Token issuer 'LocalAuth' is not trusted
```

### Causes

#### 1. Issuer Format Incorrect

**Problem**: Using friendly name instead of full URL.

```javascript
// Token Generation
issuer: 'LocalAuth'  // ❌ Wrong! This is a name, not a URL

// Validator Configuration
issuer: 'https://localhost:4000'  // Expects full URL
```

**Solution**: Use full URL format in token generation.

```javascript
// ✅ Correct
const options = {
  issuer: 'https://localhost:4000'
};
```

#### 2. Issuer Not Configured

**Problem**: Token's `iss` claim doesn't match any configured issuer.

```javascript
// Token has: "iss": "https://auth.example.com"

// But configuration only has:
issuers: [
  {
    issuer: 'https://localhost:4000'  // ❌ Doesn't match!
  }
]
```

**Solution**: Add the issuer to your configuration.

```javascript
issuers: [
  {
    name: 'ExampleAuth',
    type: 'jwt',
    issuer: 'https://auth.example.com',  // ✅ Matches token
    secret: '...',
    audiences: ['...']
  }
]
```

#### 3. Case Sensitivity

**Problem**: Issuer URLs are case-sensitive.

```javascript
// Token: "iss": "https://localhost:4000"
// Config: issuer: "https://LocalHost:4000"  // ❌ Case mismatch!
```

**Solution**: Ensure exact case match.

---

## Invalid Audience

### Error Message
```
jwt audience invalid. expected: api://your-app-id
```
or
```
Audience validation failed
```

### Causes

#### 1. Audience Format Mismatch

**Problem**: Using URL instead of API identifier format.

```javascript
// Token Generation
audience: 'http://localhost:4000'  // ❌ Wrong format

// Validator Configuration
audiences: ['api://your-app-id']
```

**Solution**: Use API identifier format.

```javascript
// ✅ Correct
const options = {
  audience: 'api://your-app-id'
};
```

#### 2. Audience Not in Allowed List

**Problem**: Token's `aud` claim not in configured audiences array.

```javascript
// Token has: "aud": "api://app-123"

// But configuration only allows:
audiences: ['api://app-456']  // ❌ Doesn't match!
```

**Solution**: Add the audience to the allowed list.

```javascript
audiences: [
  'api://app-123',  // ✅ Now allowed
  'api://app-456'
]
```

---

## Token Expired

### Error Message
```
jwt expired
```
or
```
TokenExpiredError: jwt expired
```

### Causes

#### 1. Token Actually Expired

**Problem**: Token's `exp` (expiration) claim is in the past.

**Solution**: Generate a new token or increase expiration time.

```javascript
// ✅ Set appropriate expiration
const options = {
  expiresIn: '1h'  // 1 hour from now
};
```

#### 2. Clock Skew Issues

**Problem**: Server clocks are out of sync.

**Solution**: Increase `clockSkew` tolerance.

```javascript
const primusAuth = primusIdentityMiddleware({
  issuers: [...],
  clockSkew: 600  // 10 minutes tolerance (in seconds)
});
```

#### 3. Expiration Format Issues

**Problem**: Using incorrect expiration format.

```javascript
// ❌ Wrong: Manual timestamp calculation might be off
exp: Date.now() + 3600  // Wrong units!

// ✅ Correct: Use expiresIn option
const options = {
  expiresIn: '1h'  // Let library handle it
};
```

---

## Missing Required Configuration

### Error Message
```
Authority URL required for OIDC issuer
```

### Cause

OIDC issuer missing `authority` property.

```javascript
{
  name: 'AzureAD',
  type: 'oidc',
  issuer: 'https://login.microsoftonline.com/TENANT/v2.0'
  // ❌ Missing: authority
}
```

### Solution

Add `authority` URL for OIDC issuers.

```javascript
{
  name: 'AzureAD',
  type: 'oidc',
  issuer: 'https://login.microsoftonline.com/TENANT/v2.0',
  authority: 'https://login.microsoftonline.com/TENANT/v2.0',  // ✅ Added
  audiences: ['api://your-app-id']
}
```

---

### Error Message
```
Shared secret required for JWT issuer
```

### Cause

JWT issuer missing `secret` property.

```javascript
{
  name: 'LocalAuth',
  type: 'jwt',
  issuer: 'https://localhost:4000'
  // ❌ Missing: secret
}
```

### Solution

Provide shared secret for JWT issuers.

```javascript
{
  name: 'LocalAuth',
  type: 'jwt',
  issuer: 'https://localhost:4000',
  secret: process.env.JWT_SECRET,  // ✅ Added
  audiences: ['api://your-app-id']
}
```

---

## JWKS Fetch Failures

### Error Message
```
Failed to fetch JWKS
```
or
```
Error fetching JWKS from authority
```

### Causes

#### 1. Network Issues

**Problem**: Cannot reach JWKS endpoint.

**Solution**: 
- Check network connectivity
- Verify firewall rules
- Ensure DNS resolution works
- Check if behind a proxy (configure proxy if needed)

#### 2. Invalid Authority URL

**Problem**: Authority URL is incorrect.

```javascript
{
  authority: 'https://login.microsoftonline.com/WRONG_TENANT/v2.0'
}
```

**Solution**: Verify tenant ID is correct.

#### 3. HTTPS Issues

**Problem**: SSL certificate validation failing.

**Solution** (Development only):
```javascript
// ⚠️ Development only! Never use in production!
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
```

For production, ensure valid SSL certificates.

---

## Debugging Tips

### 1. Enable Debug Logging

```javascript
// Add debug middleware before primusAuth
app.use((req, res, next) => {
  console.log('Authorization Header:', req.headers.authorization);
  next();
});

app.use(primusAuth);

// Or use a logging library
const morgan = require('morgan');
app.use(morgan('dev'));
```

### 2. Decode Token at jwt.io

Visit [https://jwt.io](https://jwt.io) and paste your token to inspect:
- Header (algorithm, key ID)
- Payload (iss, aud, sub, exp claims)
- Signature validity

### 3. Check Token Claims

```javascript
app.get('/debug', primusAuth, (req, res) => {
  res.json({
    user: req.primusUser,
    rawClaims: req.primusUser?.additionalClaims
  });
});
```

### 4. Validate Configuration Match

Create a debug endpoint to verify configuration:

```javascript
app.get('/config-debug', (req, res) => {
  res.json({
    issuer: process.env.JWT_ISSUER,
    audience: process.env.JWT_AUDIENCE,
    secretLength: process.env.JWT_SECRET?.length
  });
});
```

### 5. Test Token Generation Separately

```javascript
const jwt = require('jsonwebtoken');

// Generate token
const token = generateToken();

// Immediately try to validate it
try {
  const decoded = jwt.verify(token, process.env.JWT_SECRET, {
    issuer: process.env.JWT_ISSUER,
    audience: process.env.JWT_AUDIENCE
  });
  console.log('✅ Token is valid:', decoded);
} catch (error) {
  console.log('❌ Validation failed:', error.message);
}
```

### 6. Inspect Token Without Verification

```javascript
const jwt = require('jsonwebtoken');

// Decode without verifying (for debugging only!)
const decoded = jwt.decode(token, { complete: true });
console.log('Header:', decoded.header);
console.log('Payload:', decoded.payload);
```

### 7. Test with curl

```bash
# Test protected endpoint
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" \
     http://localhost:4000/api/protected
```

---

## Quick Diagnostic Checklist

When you encounter a validation error:

- [ ] Decode token at jwt.io to inspect claims
- [ ] Verify `iss` claim matches configured `issuer` exactly
- [ ] Verify `aud` claim is in configured `audiences` array
- [ ] Verify `exp` claim is in the future (Unix timestamp)
- [ ] Check secret key matches (for Local JWT)
- [ ] Check algorithm is `HS256` (for Local JWT) or `RS256` (for Azure AD)
- [ ] Verify environment variables are loaded correctly
- [ ] Check Authorization header format: `Bearer <token>`
- [ ] Test token generation and validation in isolation
- [ ] Check server logs for detailed error messages

---

## Common Error Patterns

### Pattern 1: Configuration Not Loaded

```javascript
// ❌ Environment variables not loaded
const secret = process.env.JWT_SECRET;  // undefined!

// ✅ Load dotenv first
require('dotenv').config();
const secret = process.env.JWT_SECRET;
```

### Pattern 2: Bearer Prefix Issues

```javascript
// ❌ Token includes "Bearer " prefix
const token = req.headers.authorization;  // "Bearer eyJ..."
jwt.verify(token, secret);  // Fails!

// ✅ Strip "Bearer " prefix
const token = req.headers.authorization?.replace('Bearer ', '');
jwt.verify(token, secret);  // Works!
```

### Pattern 3: Async/Await Issues

```javascript
// ❌ Not awaiting async validation
app.get('/api/data', primusAuth, (req, res) => {
  // req.primusUser might not be set yet!
});

// ✅ Middleware handles async properly
// Just use req.primusUser directly - middleware waits for validation
app.get('/api/data', primusAuth, (req, res) => {
  res.json({ user: req.primusUser });  // Safe to use
});
```

---

## Need More Help?

- See [TOKEN_GENERATION_GUIDE.md](./TOKEN_GENERATION_GUIDE.md) for token generation examples
- See [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md) for production best practices
- GitHub Issues: https://github.com/akkikhan/Primus-SaaS/issues
- Documentation: https://portal.primus-saas.com/docs

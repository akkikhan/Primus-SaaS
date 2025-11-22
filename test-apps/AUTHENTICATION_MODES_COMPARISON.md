# 🔐 Primus SaaS Authentication Modes Comparison

**Understanding Local, Azure AD, and Hybrid modes**

---

## 📊 Quick Comparison Table

| Feature | Local Mode | Azure AD Mode | Hybrid Mode |
|---------|-----------|---------------|-------------|
| **Token Issuer** | Primus Portal | Azure AD | Both |
| **Validation Method** | HMAC/JWT Secret | Azure AD Public Keys | Try Azure, fallback Local |
| **Internet Required** | ❌ No | ✅ Yes | ⚠️ Partial |
| **Setup Complexity** | ⭐ Simple | ⭐⭐⭐ Complex | ⭐⭐⭐ Complex |
| **Enterprise SSO** | ❌ No | ✅ Yes | ✅ Yes |
| **Multi-factor Auth** | ❌ No | ✅ Yes (via Azure) | ✅ Yes (via Azure) |
| **Offline Support** | ✅ Yes | ❌ No | ⚠️ Partial |
| **Performance** | ⚡ Fast | 🐌 Slower (key fetch) | 🐌 Slower |
| **Best For** | Simple apps, testing | Enterprise apps | Migration period |

---

## 🎯 Mode 1: Local Mode

### How It Works

```javascript
const PRIMUS_CONFIG = {
    portalUrl: 'http://localhost:5267',
    clientId: 'PSP-CLI-711224',
    clientSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    jwtSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    mode: 'Local'  // ✅ Local mode
};
```

### Authentication Flow

```
1. User logs in with email/password
   ↓
2. Primus Portal validates credentials
   ↓
3. Primus Portal generates JWT token (signed with jwtSecret)
   ↓
4. User sends JWT token with API requests
   ↓
5. Acme Dashboard validates JWT using jwtSecret
   ↓
6. Protected data returned
```

### Token Structure

```json
{
  "sub": "12345",
  "email": "user@example.com",
  "name": "John Doe",
  "role": "Admin",
  "iss": "http://localhost:5267",
  "aud": "PSP-CLI-711224",
  "exp": 1732256400,
  "iat": 1732252800
}
```

**Signature**: HMAC-SHA256 using `jwtSecret`

### Pros ✅

- **Simple setup**: Just need client ID and secret
- **Fast validation**: No external API calls
- **Works offline**: No internet required
- **Low latency**: Instant token validation
- **Easy debugging**: Tokens can be decoded at jwt.io

### Cons ❌

- **No SSO**: Users must create separate accounts
- **No MFA**: Basic password authentication only
- **Manual user management**: Must manage users in Primus Portal
- **No enterprise features**: No group policies, conditional access, etc.

### Use Cases

- ✅ Development and testing
- ✅ Simple applications
- ✅ Internal tools
- ✅ Proof of concepts
- ❌ Enterprise applications
- ❌ Apps requiring SSO

---

## 🎯 Mode 2: Azure AD Mode

### How It Works

```javascript
const PRIMUS_CONFIG = {
    portalUrl: 'http://localhost:5267',
    clientId: 'PSP-CLI-711224',
    clientSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    jwtSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    mode: 'AzureAd',  // ✅ Azure AD mode
    tenantId: 'cbd15a9b-cd52-4ccc-916a-00e2edb13043'
};
```

### Authentication Flow

```
1. User clicks "Sign in with Microsoft"
   ↓
2. Redirect to Azure AD login page
   ↓
3. User enters Azure AD credentials (+ MFA if enabled)
   ↓
4. Azure AD validates and returns ID token
   ↓
5. ID token sent to Primus Portal
   ↓
6. Primus Portal validates token with Azure AD
   ↓
7. Primus Portal returns session JWT
   ↓
8. User sends JWT with API requests
   ↓
9. Acme Dashboard validates JWT using Azure AD public keys
   ↓
10. Protected data returned
```

### Token Structure

```json
{
  "aud": "c28b195b-8396-42e6-bc6f-7773736dfa40",
  "iss": "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0",
  "iat": 1732252800,
  "nbf": 1732252800,
  "exp": 1732256400,
  "email": "user@company.com",
  "name": "John Doe",
  "oid": "00000000-0000-0000-0000-000000000000",
  "preferred_username": "user@company.com",
  "rh": "...",
  "sub": "...",
  "tid": "cbd15a9b-cd52-4ccc-916a-00e2edb13043",
  "uti": "...",
  "ver": "2.0"
}
```

**Signature**: RSA-SHA256 using Azure AD private key (validated with public keys)

### Validation Process

```javascript
// 1. Fetch Azure AD public keys (cached for 24 hours)
const keys = await fetch(
  `https://login.microsoftonline.com/${tenantId}/discovery/v2.0/keys`
);

// 2. Find the key matching the token's 'kid' (key ID)
const signingKey = keys.find(k => k.kid === token.header.kid);

// 3. Verify signature using public key
const decoded = jwt.verify(token, signingKey.publicKey);

// 4. Verify claims
if (decoded.tid !== tenantId) throw new Error('Invalid tenant');
if (decoded.aud !== clientId) throw new Error('Invalid audience');
if (decoded.exp < Date.now() / 1000) throw new Error('Token expired');

// 5. Extract user info
const user = {
  userId: decoded.sub,
  email: decoded.email,
  name: decoded.name,
  roles: decoded.roles || []
};
```

### Pros ✅

- **Enterprise SSO**: Users sign in with existing Azure AD accounts
- **Multi-factor authentication**: Leverage Azure AD MFA
- **Conditional access**: Apply Azure AD policies
- **Group-based access**: Map Azure AD groups to roles
- **Audit logs**: Azure AD tracks all sign-ins
- **No password management**: Azure AD handles it
- **Compliance**: Meets enterprise security requirements

### Cons ❌

- **Complex setup**: Requires Azure AD app registration
- **Internet required**: Must fetch public keys from Azure
- **Slower validation**: External API calls for key fetching
- **Azure dependency**: Requires Azure AD subscription
- **Debugging harder**: Tokens are more complex

### Use Cases

- ✅ Enterprise applications
- ✅ Apps requiring SSO
- ✅ Organizations using Microsoft 365
- ✅ Apps needing MFA
- ✅ Compliance-heavy industries
- ❌ Simple internal tools
- ❌ Offline applications

---

## 🎯 Mode 3: Hybrid Mode

### How It Works

```javascript
const PRIMUS_CONFIG = {
    portalUrl: 'http://localhost:5267',
    clientId: 'PSP-CLI-711224',
    clientSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    jwtSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    mode: 'Hybrid',  // ✅ Hybrid mode
    tenantId: 'cbd15a9b-cd52-4ccc-916a-00e2edb13043'
};
```

### Authentication Flow

```
1. User sends token with API request
   ↓
2. Acme Dashboard receives token
   ↓
3. Try Azure AD validation first
   ├─ Success? → Return user data
   └─ Failed? → Try Local validation
      ├─ Success? → Return user data
      └─ Failed? → Return 401 Unauthorized
```

### Validation Logic

```javascript
async function validateToken(token, options) {
  // Try Azure AD first
  try {
    const azureResult = await validateAzureAdToken(token, options);
    if (azureResult.isValid) {
      return azureResult;
    }
  } catch (error) {
    console.log('Azure AD validation failed, trying Local...');
  }
  
  // Fallback to Local
  try {
    const localResult = await validateLocalToken(token, options);
    return localResult;
  } catch (error) {
    throw new Error('Token validation failed');
  }
}
```

### Pros ✅

- **Flexibility**: Supports both Azure AD and local users
- **Migration friendly**: Gradually move users to Azure AD
- **Fallback support**: Works if Azure AD is down
- **Mixed environments**: Some users SSO, others local
- **Testing**: Test Azure AD in production alongside local

### Cons ❌

- **Complexity**: Most complex mode to manage
- **Performance**: Tries Azure AD first (slower)
- **Security confusion**: Two authentication methods
- **Harder debugging**: Which method validated the token?
- **Maintenance**: Must maintain both systems

### Use Cases

- ✅ Migration from Local to Azure AD
- ✅ Mixed user bases (employees + external users)
- ✅ Testing Azure AD in production
- ✅ Gradual rollout of SSO
- ❌ New applications (choose one mode)
- ❌ Simple use cases

---

## 🔧 Configuration Requirements

### Local Mode

**Required**:
- ✅ `portalUrl`
- ✅ `clientId`
- ✅ `clientSecret`
- ✅ `jwtSecret`

**Optional**:
- `issuer` (defaults to `portalUrl`)
- `audience` (defaults to `clientId`)
- `validateLifetime` (defaults to `true`)
- `clockSkew` (defaults to 300 seconds)

### Azure AD Mode

**Required**:
- ✅ `portalUrl`
- ✅ `clientId`
- ✅ `clientSecret`
- ✅ `jwtSecret`
- ✅ `tenantId`

**Optional**:
- `audience` (defaults to `clientId`)
- `validateLifetime` (defaults to `true`)
- `clockSkew` (defaults to 300 seconds)
- `jwksCacheTtl` (defaults to 24 hours)

### Hybrid Mode

**Required**:
- ✅ `portalUrl`
- ✅ `clientId`
- ✅ `clientSecret`
- ✅ `jwtSecret`
- ✅ `tenantId`

**Optional**: Same as Azure AD mode

---

## 🎓 Choosing the Right Mode

### Decision Tree

```
Do you need enterprise SSO?
├─ YES → Do you use Microsoft 365/Azure AD?
│         ├─ YES → Use Azure AD Mode
│         └─ NO → Consider other SSO providers (not covered here)
└─ NO → Is this for development/testing?
          ├─ YES → Use Local Mode
          └─ NO → Do you need to support both SSO and local users?
                  ├─ YES → Use Hybrid Mode
                  └─ NO → Use Local Mode
```

### Recommendations

**Start with Local Mode if**:
- Building a proof of concept
- Internal tool with few users
- No enterprise requirements
- Want simple, fast setup

**Use Azure AD Mode if**:
- Enterprise application
- Users already have Azure AD accounts
- Need MFA or conditional access
- Compliance requirements

**Use Hybrid Mode if**:
- Migrating from Local to Azure AD
- Need to support both user types
- Testing Azure AD before full rollout
- Mixed internal/external users

---

## 📊 Performance Comparison

### Token Validation Time

| Mode | First Request | Cached Request | Offline |
|------|--------------|----------------|---------|
| **Local** | ~1ms | ~1ms | ✅ Works |
| **Azure AD** | ~200-500ms | ~1-5ms | ❌ Fails |
| **Hybrid** | ~200-500ms | ~1-5ms | ⚠️ Fallback to Local |

**Note**: Azure AD mode caches public keys for 24 hours, so subsequent requests are much faster.

---

## 🔐 Security Comparison

### Attack Surface

| Aspect | Local | Azure AD | Hybrid |
|--------|-------|----------|--------|
| **Secret Exposure** | High risk | Low risk | High risk |
| **Brute Force** | Possible | Protected by Azure | Possible (Local) |
| **MFA** | ❌ No | ✅ Yes | ⚠️ Only Azure users |
| **Conditional Access** | ❌ No | ✅ Yes | ⚠️ Only Azure users |
| **Token Replay** | Possible | Mitigated | Possible |
| **Key Rotation** | Manual | Automatic | Manual (Local) |

**Recommendation**: For production enterprise apps, use Azure AD mode for better security.

---

## 🚀 Migration Path

### From Local to Azure AD

**Phase 1: Preparation**
1. Set up Azure AD app registration
2. Configure Primus Portal with Azure AD settings
3. Test Azure AD login in development

**Phase 2: Hybrid Rollout**
1. Switch to Hybrid mode
2. Existing users continue with Local
3. New users sign up with Azure AD
4. Gradually migrate existing users

**Phase 3: Full Azure AD**
1. All users migrated to Azure AD
2. Switch to Azure AD mode
3. Remove Local authentication
4. Decommission local user database

---

## 📝 Summary

| Choose This | If You Need |
|-------------|-------------|
| **Local** | Simple, fast, offline authentication |
| **Azure AD** | Enterprise SSO, MFA, compliance |
| **Hybrid** | Support both during migration |

**Most Common Choice**: Start with **Local** for development, move to **Azure AD** for production enterprise apps.

---

**Questions?** Check the full integration guide or test with the provided scripts!

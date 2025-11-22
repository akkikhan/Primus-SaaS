# ✅ Corrected Azure AD Flow (Aligned with Standard Practice)

**Based on the architecture diagram discussion**

---

## 🔄 The Correct Flow

### **Standard Azure AD Integration (Recommended)**

```
┌─────────────┐         ┌──────────────┐         ┌──────────────┐
│   Browser   │         │    Acme      │         │  Azure AD    │
│   (User)    │         │  Dashboard   │         │  (Microsoft) │
└──────┬──────┘         └──────┬───────┘         └──────┬───────┘
       │                       │                        │
       │ 1. Click "Login with Microsoft"                │
       ├──────────────────────►│                        │
       │                       │                        │
       │ 2. Redirect to Azure AD /authorize             │
       ├────────────────────────────────────────────────►│
       │                                                 │
       │ 3. Show Microsoft login page                   │
       │◄────────────────────────────────────────────────┤
       │                                                 │
       │ 4. Enter credentials & consent                 │
       ├────────────────────────────────────────────────►│
       │                                                 │
       │ 5. Redirect to app with id_token/access_token  │
       │◄────────────────────────────────────────────────┤
       │                       │                        │
       │ 6. Frontend now holds Azure AD token           │
       │                       │                        │
       │ 7. Click "View Orders" (or any protected action)│
       │                       │                        │
       │ 8. GET /api/orders                             │
       │    Authorization: Bearer <AzureADToken>        │
       ├──────────────────────►│                        │
       │                       │                        │
       │                       │ 9. Validate Azure AD Token
       │                       │    - Fetch JWKS keys   │
       │                       │    - Verify signature  │
       │                       │    - Check issuer      │
       │                       │    - Check audience    │
       │                       │    - Check expiry      │
       │                       │                        │
       │                       │ 10. Extract claims     │
       │                       │     (email, name, roles)
       │                       │                        │
       │ 11. Return 200 OK + orders data                │
       │◄──────────────────────┤                        │
       │                       │                        │
       │ 12. Render orders page│                        │
```

---

## 🔑 Key Differences from Previous Documentation

### ❌ **What I Incorrectly Documented:**

1. Frontend sends Azure AD token to **Primus Portal** first
2. Primus Portal validates and issues **its own JWT**
3. Frontend uses **Primus JWT** for API calls
4. **Two tokens involved**: Azure AD token + Primus JWT

### ✅ **What Should Actually Happen:**

1. Frontend gets Azure AD token from Microsoft
2. Frontend sends **Azure AD token directly** to Acme Dashboard
3. Acme Dashboard validates **Azure AD token** using Primus SDK
4. **One token involved**: Azure AD token only

---

## 🔧 Configuration (Simplified)

### Acme Dashboard Configuration

**File**: `server.js`

```javascript
const PRIMUS_CONFIG = {
    portalUrl: 'http://localhost:5267',           // Not used for token validation
    clientId: 'YOUR_AZURE_CLIENT_ID',             // ✅ Azure AD Client ID (not Primus)
    clientSecret: 'YOUR_AZURE_CLIENT_SECRET',     // ✅ Azure AD Client Secret
    jwtSecret: 'not-needed-for-azure-ad',         // Not used in AzureAd mode
    mode: 'AzureAd',                              // ✅ Azure AD validation
    tenantId: 'YOUR_AZURE_TENANT_ID'              // ✅ Your Azure AD tenant
};
```

**Important Changes:**
- `clientId` should be **Azure AD Client ID** (not Primus Client ID)
- `clientSecret` should be **Azure AD Client Secret** (if needed)
- No need for Primus Portal in the authentication flow

---

## 🎯 What the Primus SDK Does

When configured with `mode: 'AzureAd'`, the SDK:

```javascript
// 1. Receives token from Authorization header
const token = req.headers.authorization.replace('Bearer ', '');

// 2. Fetches Azure AD public keys (JWKS)
const jwksUrl = `https://login.microsoftonline.com/${tenantId}/discovery/v2.0/keys`;
const keys = await fetch(jwksUrl).then(r => r.json());

// 3. Finds the correct key (matching 'kid' in token header)
const signingKey = keys.keys.find(k => k.kid === tokenHeader.kid);

// 4. Verifies token signature
const decoded = jwt.verify(token, signingKey.publicKey, {
    algorithms: ['RS256']
});

// 5. Validates claims
if (decoded.tid !== tenantId) throw new Error('Invalid tenant');
if (decoded.aud !== clientId) throw new Error('Invalid audience');
if (decoded.exp < Date.now() / 1000) throw new Error('Token expired');

// 6. Extracts user info
req.primusUser = {
    userId: decoded.sub || decoded.oid,
    email: decoded.email || decoded.preferred_username,
    name: decoded.name,
    roles: decoded.roles || []
};
```

---

## 🌐 Frontend Implementation

### Login Flow

```javascript
// Azure AD Configuration
const AZURE_CONFIG = {
    tenantId: 'YOUR_AZURE_TENANT_ID',
    clientId: 'YOUR_AZURE_CLIENT_ID',
    redirectUri: 'http://localhost:3000/auth/callback',
    scope: 'openid profile email'
};

// 1. Redirect to Azure AD
function loginWithAzure() {
    const authUrl = `https://login.microsoftonline.com/${AZURE_CONFIG.tenantId}/oauth2/v2.0/authorize?` +
        `client_id=${AZURE_CONFIG.clientId}` +
        `&response_type=id_token` +
        `&redirect_uri=${encodeURIComponent(AZURE_CONFIG.redirectUri)}` +
        `&scope=${encodeURIComponent(AZURE_CONFIG.scope)}` +
        `&response_mode=fragment` +
        `&nonce=${generateNonce()}`;
    
    window.location.href = authUrl;
}

// 2. Handle callback
function handleAzureCallback() {
    const hash = window.location.hash.substring(1);
    const params = new URLSearchParams(hash);
    const idToken = params.get('id_token');
    
    // ✅ Store Azure AD token directly
    localStorage.setItem('authToken', idToken);
    
    // Redirect to dashboard
    window.location.href = '/dashboard.html';
}

// 3. Make API calls with Azure AD token
async function fetchOrders() {
    const token = localStorage.getItem('authToken');
    
    const response = await fetch('http://localhost:3000/api/orders', {
        headers: {
            'Authorization': `Bearer ${token}`  // ✅ Azure AD token
        }
    });
    
    return response.json();
}
```

---

## 🔄 When to Use Primus Portal

### **Primus Portal is for:**

1. **Managing applications** (create apps, get credentials)
2. **Managing modules** (IdentityValidator, etc.)
3. **Generating documentation** for client developers
4. **SDK distribution** (npm/NuGet packages)
5. **Local authentication** (when not using Azure AD)

### **Primus Portal is NOT for:**

- ❌ Validating Azure AD tokens at runtime (SDK does this)
- ❌ Issuing secondary tokens (use Azure AD tokens directly)
- ❌ Being in the authentication flow for Azure AD mode

---

## 📊 Comparison: Two Approaches

### **Approach 1: Direct Azure AD (Standard - Recommended)**

```
User → Azure AD → Azure Token → Acme Dashboard → Validate → Data
```

**Pros:**
- ✅ Standard OAuth 2.0 / OIDC flow
- ✅ Fewer moving parts
- ✅ Lower latency
- ✅ Industry best practice

**Cons:**
- ❌ Less control over token format
- ❌ Tied to Azure AD token lifetime

### **Approach 2: Two-Token Flow (Custom)**

```
User → Azure AD → Azure Token → Primus Portal → Primus JWT → Acme Dashboard → Validate → Data
```

**Pros:**
- ✅ More control over session management
- ✅ Can customize token claims
- ✅ Can implement custom expiration

**Cons:**
- ❌ Extra network hop
- ❌ Higher latency
- ❌ More complex
- ❌ Non-standard flow

---

## 🎯 Recommended Configuration

### For Azure AD Mode (Standard Flow)

**Acme Dashboard** (`server.js`):

```javascript
const PRIMUS_CONFIG = {
    // These are for SDK metadata only
    portalUrl: 'http://localhost:5267',
    
    // ✅ Use Azure AD credentials
    clientId: 'c28b195b-8396-42e6-bc6f-7773736dfa40',      // Azure AD Client ID
    clientSecret: 'not-needed-for-token-validation',       // Not used
    jwtSecret: 'not-needed-for-azure-ad',                  // Not used
    
    // ✅ Azure AD configuration
    mode: 'AzureAd',
    tenantId: 'cbd15a9b-cd52-4ccc-916a-00e2edb13043'       // Azure AD Tenant ID
};

// ✅ Protected endpoint - validates Azure AD tokens directly
app.get('/api/orders', primusAuth, (req, res) => {
    // req.primusUser contains info from Azure AD token
    res.json({
        orders: [...],
        user: req.primusUser
    });
});
```

**Frontend** (`auth.js`):

```javascript
// ✅ Use Azure AD token directly
const token = localStorage.getItem('authToken');  // Azure AD token

fetch('/api/orders', {
    headers: {
        'Authorization': `Bearer ${token}`  // Send Azure AD token
    }
});
```

---

## 🧪 Testing with Azure CLI

```powershell
# 1. Get Azure AD token
$token = (az account get-access-token --resource YOUR_AZURE_CLIENT_ID | ConvertFrom-Json).accessToken

# 2. Call Acme Dashboard API directly (no Primus Portal step)
Invoke-RestMethod -Uri "http://localhost:3000/api/orders" `
    -Headers @{ "Authorization" = "Bearer $token" }
```

**Expected:**
- ✅ Token validated by Primus SDK
- ✅ User info extracted from Azure AD token
- ✅ Protected data returned

---

## 📝 Summary

### **The Correct Flow:**

1. User authenticates with **Azure AD** (Microsoft login page)
2. Azure AD returns **ID token** to frontend
3. Frontend stores **Azure AD token**
4. Frontend sends **Azure AD token** to Acme Dashboard API
5. Primus SDK validates **Azure AD token** (fetches keys, verifies signature)
6. Protected data returned

### **Primus Portal's Role:**

- Manages applications and modules
- Provides SDK packages
- Generates documentation
- **NOT involved in runtime authentication for Azure AD mode**

### **Key Takeaway:**

> In Azure AD mode, the Primus SDK validates Azure AD tokens **directly** without involving Primus Portal. The portal is only for **management and documentation**, not runtime authentication.

---

**This aligns with the ChatGPT diagram and standard OAuth 2.0 / OIDC practices!** ✅

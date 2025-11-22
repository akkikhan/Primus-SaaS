# 🔐 Azure AD Credentials for Acme Dashboard

**Created**: November 22, 2025  
**Status**: ✅ Ready to Use

---

## 📋 Azure AD App Registration Details

### **Application Information**

| Property | Value |
|----------|-------|
| **Display Name** | Acme Dashboard |
| **Application (Client) ID** | `acc675f1-e32f-40b9-a0c6-716066cc6890` |
| **Directory (Tenant) ID** | `cbd15a9b-cd52-4ccc-916a-00e2edb13043` |
| **Object ID** | `75bb39ae-1f74-449b-91ae-90477f1e9f3c` |
| **Tenant Domain** | `akkhan2026outlook.onmicrosoft.com` |
| **Sign-in Audience** | AzureADMyOrg (Single tenant) |

### **API Configuration**

| Property | Value |
|----------|-------|
| **Application ID URI** | `api://acc675f1-e32f-40b9-a0c6-716066cc6890` |
| **API Scope** | `user_impersonation` |
| **Full Scope** | `api://acc675f1-e32f-40b9-a0c6-716066cc6890/user_impersonation` |

### **Redirect URIs**

| Type | URI |
|------|-----|
| Web (SPA) | `http://localhost:3000` |
| Web (SPA) | `http://localhost:3000/auth/callback` |

### **Token Configuration**

| Setting | Enabled |
|---------|---------|
| **ID Tokens** | ✅ Yes |
| **Access Tokens** | ✅ Yes |

---

## 🔧 Configuration Applied

### **Frontend (MSAL)**

File: `public/js/msal-config.js`

```javascript
const msalConfig = {
    auth: {
        clientId: "acc675f1-e32f-40b9-a0c6-716066cc6890",
        authority: "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043",
        redirectUri: window.location.origin
    }
};

const tokenRequest = {
    scopes: [
        "api://acc675f1-e32f-40b9-a0c6-716066cc6890/user_impersonation"
    ]
};
```

### **Backend (Primus SDK)**

File: `server.js`

```javascript
const PRIMUS_CONFIG = {
    defaultAuthority: 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043',
    allowedAudiences: ['api://acc675f1-e32f-40b9-a0c6-716066cc6890'],
    primusTrackingId: 'PSP-CLI-711224',
    portalUrl: 'http://localhost:5267'
};
```

---

## 🚀 How to Use

### **1. Start the Server**

```bash
cd test-apps/acme-dashboard
node server.js
```

**Expected Output**:
```
🏦 ACME Financial Dashboard Server Starting...
✅ Primus Identity Validator configured
   - Authority: https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043
   - Allowed Audiences: [ 'api://acc675f1-e32f-40b9-a0c6-716066cc6890' ]
   - Tracking ID: PSP-CLI-711224
   - Portal URL: http://localhost:5267
🚀 Acme Dashboard running at http://localhost:3000
📝 API Endpoints:
   - GET /api/revenue-stats (protected)
   - GET /api/health (public)
```

### **2. Open in Browser**

```
http://localhost:3000
```

### **3. Login**

1. Click **"Sign in with Microsoft"**
2. Popup window opens
3. Login with: **akkhan2026@outlook.com**
4. Grant consent (first time only)
5. Dashboard loads

---

## 🧪 Testing

### **Test 1: Health Check** (No Auth)

```bash
curl http://localhost:3000/api/health
```

**Expected**:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-22T01:15:00.000Z"
}
```

### **Test 2: Protected Endpoint** (With Auth)

1. Login to dashboard
2. Open browser console (F12)
3. Run:

```javascript
const accounts = msalInstance.getAllAccounts();
const response = await msalInstance.acquireTokenSilent({
    scopes: ["api://acc675f1-e32f-40b9-a0c6-716066cc6890/user_impersonation"],
    account: accounts[0]
});

const apiResponse = await fetch('/api/revenue-stats', {
    headers: { 'Authorization': `Bearer ${response.accessToken}` }
});

const data = await apiResponse.json();
console.log('API Response:', data);
```

**Expected**:
```json
{
  "company": "Acme Corp",
  "revenue": "$4,250,000",
  "growth": "+125%",
  "activeUsers": 14500,
  "lastUpdated": "2025-11-22T01:15:00.000Z",
  "user": {
    "userId": "...",
    "email": "akkhan2026@outlook.com",
    ...
  }
}
```

---

## 🔍 Verification

### **Check Azure AD App**

```bash
# View app details
az ad app show --id acc675f1-e32f-40b9-a0c6-716066cc6890

# View API permissions
az ad app permission list --id acc675f1-e32f-40b9-a0c6-716066cc6890
```

### **Check Token**

After login, check the token in browser console:

```javascript
const accounts = msalInstance.getAllAccounts();
const response = await msalInstance.acquireTokenSilent({
    scopes: ["api://acc675f1-e32f-40b9-a0c6-716066cc6890/user_impersonation"],
    account: accounts[0]
});

console.log('Access Token:', response.accessToken);

// Decode token (JWT)
const parts = response.accessToken.split('.');
const payload = JSON.parse(atob(parts[1]));
console.log('Token Payload:', payload);
```

**Verify**:
- ✅ `aud` (audience) = `api://acc675f1-e32f-40b9-a0c6-716066cc6890`
- ✅ `iss` (issuer) = `https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0`
- ✅ `scp` (scopes) = `user_impersonation`

---

## 📊 Architecture Flow

```
User (akkhan2026@outlook.com)
  ↓
Azure AD (cbd15a9b-cd52-4ccc-916a-00e2edb13043)
  ↓
MSAL.js (Frontend)
  ├─ Client ID: acc675f1-e32f-40b9-a0c6-716066cc6890
  └─ Gets access_token
  ↓
API Call
  GET /api/revenue-stats
  Authorization: Bearer <access_token>
  ↓
Primus IdentityValidator (Backend)
  ├─ Fetches JWKS from Azure AD
  ├─ Validates signature
  ├─ Validates issuer, audience, expiry
  └─ Attaches req.primusUser
  ↓
API Controller
  ├─ Access req.primusUser
  └─ Returns protected data
```

**⚠️ Primus Portal is NOT in this flow!**

---

## 🎯 Success Criteria

- [x] Azure AD app created
- [x] API scope configured
- [x] Redirect URIs set
- [x] Frontend configured with credentials
- [x] Backend configured with credentials
- [x] Server starts without errors
- [x] Can login with Microsoft
- [x] Protected endpoint works
- [x] Token validation works locally

---

## 🔒 Security Notes

1. **Credentials are real** - Don't commit to public repos
2. **Single tenant** - Only your Azure AD users can login
3. **Local validation** - Portal not involved in auth
4. **Tokens in memory** - sessionStorage (secure)

---

## 📚 References

- **Azure Portal**: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Overview/appId/acc675f1-e32f-40b9-a0c6-716066cc6890
- **MSAL.js Docs**: https://github.com/AzureAD/microsoft-authentication-library-for-js
- **Setup Guide**: `SETUP_GUIDE.md`

---

**✅ Ready to test! Start the server and open http://localhost:3000** 🚀

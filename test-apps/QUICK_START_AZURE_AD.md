# 🚀 Quick Start: Azure AD Integration Changes

**What you need**: Azure AD Tenant ID and Client ID  
**Time required**: 5 minutes  
**Current status**: Acme Dashboard is already configured for Azure AD mode, just needs tenant ID update

---

## 📝 Required Changes

### 1. Primus Portal Backend Configuration

**File**: `c:\Users\aakib\Primus SaaS\portal\backend\appsettings.json`

**Find this section**:
```json
"AzureAd": {
  "TenantId": "cbd15a9b-cd52-4ccc-916a-00e2edb13043",
  "ClientId": "c28b195b-8396-42e6-bc6f-7773736dfa40",
  "Audience": "c28b195b-8396-42e6-bc6f-7773736dfa40"
}
```

**Replace with YOUR Azure AD credentials**:
```json
"AzureAd": {
  "TenantId": "YOUR_AZURE_TENANT_ID_HERE",
  "ClientId": "YOUR_AZURE_CLIENT_ID_HERE",
  "Audience": "YOUR_AZURE_CLIENT_ID_HERE"
}
```

**Then restart the backend**:
```powershell
# Press Ctrl+C in the terminal running the backend
# Then restart:
cd "c:\Users\aakib\Primus SaaS\portal\backend"
dotnet run
```

---

### 2. Acme Dashboard Configuration

**File**: `c:\Users\aakib\Primus SaaS\test-apps\acme-dashboard\server.js`

**Find this section** (around line 17-24):
```javascript
const PRIMUS_CONFIG = {
    portalUrl: 'http://localhost:5267',
    clientId: 'PSP-CLI-711224',
    clientSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    jwtSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    mode: 'AzureAd',
    tenantId: 'common'  // ⚠️ CHANGE THIS
};
```

**Update the `tenantId`**:
```javascript
const PRIMUS_CONFIG = {
    portalUrl: 'http://localhost:5267',
    clientId: 'PSP-CLI-711224',
    clientSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    jwtSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    mode: 'AzureAd',
    tenantId: 'YOUR_AZURE_TENANT_ID_HERE'  // ✅ Your actual tenant ID
};
```

**Then restart the server**:
```powershell
# Press Ctrl+C in the terminal running the server
# Then restart:
cd "c:\Users\aakib\Primus SaaS\test-apps\acme-dashboard"
node server.js
```

---

## 🧪 Testing with Azure CLI

### Quick Test (Copy & Paste)

**Step 1**: Update the script with your Azure Client ID

Open: `c:\Users\aakib\Primus SaaS\test-apps\test-azure-ad.ps1`

Change line 8:
```powershell
$AZURE_CLIENT_ID = "YOUR_AZURE_CLIENT_ID_HERE"
```

**Step 2**: Run the test script

```powershell
cd "c:\Users\aakib\Primus SaaS\test-apps"
.\test-azure-ad.ps1
```

This will:
1. ✅ Check if Azure CLI is installed
2. ✅ Login to Azure (if needed)
3. ✅ Get an Azure AD access token
4. ✅ Authenticate with Primus Portal
5. ✅ Test Acme Dashboard protected API
6. ✅ Verify unauthorized access is blocked

---

## 🎯 Understanding the Flow

### What Happens When You Test:

```
1. Azure CLI gets token from Azure AD
   ↓
2. Token sent to Primus Portal (/api/auth/azure)
   ↓
3. Primus Portal validates token with Azure AD
   ↓
4. Primus Portal returns its own JWT token
   ↓
5. JWT token used to access Acme Dashboard API
   ↓
6. Acme Dashboard validates JWT using Azure AD mode
   ↓
7. Protected data returned
```

### Key Points:

- **Azure AD** authenticates the user
- **Primus Portal** validates Azure tokens and issues session JWTs
- **Acme Dashboard** uses Primus SDK to validate requests
- The SDK automatically fetches Azure AD public keys to verify signatures

---

## 🔍 What Each Configuration Does

### Primus Portal (`appsettings.json`)

```json
{
  "AzureAd": {
    "TenantId": "...",    // Your Azure AD tenant (organization)
    "ClientId": "...",    // Your Azure AD app registration ID
    "Audience": "..."     // Who the token is intended for (usually same as ClientId)
  }
}
```

**Purpose**: Tells Primus Portal how to validate Azure AD tokens

### Acme Dashboard (`server.js`)

```javascript
{
  portalUrl: 'http://localhost:5267',           // Where Primus Portal is running
  clientId: 'PSP-CLI-711224',                   // Your Primus app ID
  clientSecret: 'psp_h9lckDU8Kk70PsC5...',     // Your Primus app secret
  jwtSecret: 'psp_h9lckDU8Kk70PsC5...',        // Secret for JWT validation
  mode: 'AzureAd',                              // Use Azure AD validation
  tenantId: 'YOUR_TENANT_ID'                    // Your Azure AD tenant
}
```

**Purpose**: Tells the Primus SDK how to validate incoming tokens

---

## 🔧 Validation Modes Explained

### Mode: 'Local'
- Validates tokens signed with `jwtSecret`
- No Azure AD involved
- Fast, simple, offline validation

### Mode: 'AzureAd'
- Validates tokens signed by Azure AD
- Fetches public keys from Azure AD
- Verifies tenant and audience
- Requires internet connection

### Mode: 'Hybrid'
- Tries Azure AD first
- Falls back to Local if Azure AD fails
- Best for transition periods
- Supports both token types

---

## 📊 Current Setup Status

### ✅ Already Configured:
- Primus Portal backend running
- Primus Portal has Azure AD config (needs your credentials)
- Acme Dashboard server running
- Acme Dashboard set to `mode: 'AzureAd'`
- Primus SDK installed and integrated

### ⚠️ Needs Your Input:
- Azure AD Tenant ID
- Azure AD Client ID
- Update `appsettings.json`
- Update `server.js`
- Restart both services

---

## 🎓 How the Primus SDK Works

### When a request comes in:

```javascript
app.get('/api/revenue-stats', primusAuth, (req, res) => {
    // primusAuth middleware does this:
    
    // 1. Extract token from Authorization header
    const token = req.headers.authorization.replace('Bearer ', '');
    
    // 2. Check mode (AzureAd in your case)
    if (mode === 'AzureAd') {
        // 3. Fetch Azure AD public keys
        const keys = await fetchAzureAdKeys(tenantId);
        
        // 4. Verify token signature
        const decoded = jwt.verify(token, keys);
        
        // 5. Verify tenant and audience
        if (decoded.tid !== tenantId) throw new Error('Invalid tenant');
        if (decoded.aud !== clientId) throw new Error('Invalid audience');
        
        // 6. Extract user info
        req.primusUser = {
            userId: decoded.sub,
            email: decoded.email,
            name: decoded.name,
            roles: decoded.roles
        };
    }
    
    // 7. Continue to your handler
    next();
});
```

---

## 🔐 Security Notes

### What Gets Validated:

1. **Token Signature**: Ensures token wasn't tampered with
2. **Tenant ID**: Ensures token is from your Azure AD
3. **Audience**: Ensures token is for your application
4. **Expiration**: Ensures token hasn't expired
5. **Issuer**: Ensures token is from Azure AD

### What You Control:

- **Primus Client ID/Secret**: Identifies your app to Primus Portal
- **Azure Tenant ID**: Identifies your organization
- **Azure Client ID**: Identifies your app registration in Azure AD

---

## 🚨 Common Issues & Solutions

### Issue: "Tenant ID not configured"
**Solution**: Update `appsettings.json` with your Azure AD Tenant ID

### Issue: "Invalid Azure AD token"
**Solution**: Ensure token is requested for your Azure AD Client ID:
```powershell
az account get-access-token --resource YOUR_AZURE_CLIENT_ID
```

### Issue: "Token signature verification failed"
**Solution**: Check that tenant ID matches in both:
- `appsettings.json` (Primus Portal)
- `server.js` (Acme Dashboard)

### Issue: "CORS error in browser"
**Solution**: Add redirect URI in Azure Portal:
- Azure AD → App registrations → Your app → Authentication
- Add: `http://localhost:3000/auth/callback`

---

## 📚 Additional Resources

- **Full Guide**: `AZURE_AD_INTEGRATION_GUIDE.md`
- **Test Script**: `test-azure-ad.ps1`
- **Integration Guide**: `INTEGRATION_GUIDE.md`
- **Primus Portal**: http://localhost:5267
- **Acme Dashboard**: http://localhost:3000

---

## ✅ Checklist

Before testing, ensure:

- [ ] Azure AD app registered in Azure Portal
- [ ] Azure AD Tenant ID copied
- [ ] Azure AD Client ID copied
- [ ] `appsettings.json` updated with Azure AD credentials
- [ ] Primus Portal backend restarted
- [ ] `server.js` updated with tenant ID
- [ ] Acme Dashboard server restarted
- [ ] Azure CLI installed (`az --version` works)
- [ ] Logged in to Azure (`az login` completed)

---

## 🎯 Next Steps

1. **Get your Azure AD credentials** (Tenant ID and Client ID)
2. **Update the two configuration files** (appsettings.json and server.js)
3. **Restart both services**
4. **Run the test script**: `.\test-azure-ad.ps1`
5. **Celebrate** when you see "ALL TESTS PASSED!" 🎉

---

**Need help?** Check the logs:
- Primus Portal: Terminal running `dotnet run`
- Acme Dashboard: Terminal running `node server.js`
- Azure AD: Azure Portal → Azure Active Directory → Sign-in logs

**Happy testing! 🚀**

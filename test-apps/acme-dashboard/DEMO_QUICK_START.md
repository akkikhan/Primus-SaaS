# Acme Dashboard - Quick Demo Guide

## 🚀 Quick Start (5 Minutes)

### Step 1: Start All Services

Open **3 PowerShell terminals** and run:

```powershell
# Terminal 1: Portal Frontend
cd "C:\Users\aakib\Primus SaaS\portal\frontend"
npm run dev
# Runs on: http://localhost:8080

# Terminal 2: Portal Backend  
cd "C:\Users\aakib\Primus SaaS\portal\backend"
dotnet run
# Runs on: http://localhost:5267

# Terminal 3: Acme Dashboard
cd "C:\Users\aakib\Primus SaaS\test-apps\acme-dashboard"
node server.js
# Runs on: http://localhost:3000
```

### Step 2: Test the Application

1. **Open browser** to `http://localhost:3000`
2. **Click "Login with Microsoft"** button
3. **Authenticate** with Azure AD
4. **View the dashboard** with your financial data

---

## 🎯 Demo Flow for Senior Management

### Part 1: Show the Portal (2 min)
1. Navigate to `http://localhost:8080`
2. Show Applications list
3. Create new application or show existing one
4. Show module assignment (Identity Validator)
5. Copy Client ID and configuration

### Part 2: Show the Code Integration (3 min)
1. Open `server.js` in VS Code
2. **Highlight lines 1-37** - Show Primus SDK integration:
   - Import: `require('primus-identity-validator')`
   - Configuration: Simple JSON object
   - Middleware: `primusIdentityMiddleware(PRIMUS_CONFIG)`

3. **Highlight line 48** - Show protected endpoint:
   ```javascript
   app.get('/api/revenue-stats', primusAuth, (req, res) => {
   ```
   **Key Point**: Just add `primusAuth` to protect any endpoint!

### Part 3: Live Demo (5 min)
1. Open `http://localhost:3000`
2. Show login screen
3. Click "Login with Microsoft"
4. Authenticate
5. Show dashboard with data
6. Open **Developer Console** (F12)
7. Show **Network tab** - API call with Bearer token
8. Show **Console logs** - Authentication flow

---

## 🧪 Testing Scenarios

### Test 1: Successful Authentication
```
1. Go to http://localhost:3000
2. Click "Login with Microsoft"
3. Enter credentials
4. ✅ Dashboard loads with user info
```

### Test 2: Protected API Without Token
```javascript
// Open browser console on http://localhost:3000
fetch('/api/revenue-stats').then(r => r.json()).then(console.log)
// ❌ Expected: 401 Unauthorized
```

### Test 3: Protected API With Token (After Login)
```
1. Login first
2. Dashboard loads
3. ✅ Revenue data displayed
4. Check Network tab - Authorization header present
```

---

## 📊 Key Talking Points

### Developer Experience
- **3 lines of code** to integrate
- **15 minutes** vs 2-4 weeks traditional approach
- **One middleware** protects all endpoints
- **Zero security expertise** required

### Business Value
- ⚡ **90% faster** time to market
- 🔒 **Enterprise security** out of the box
- 💰 **Cost savings** on development
- 📈 **Scalable** to unlimited applications

---

## 🔧 Troubleshooting

### Issue: Login button doesn't work
**Check**: Browser console for JavaScript errors
**Fix**: Refresh page with Ctrl+F5

### Issue: 401 on API calls
**Check**: 
- Azure AD Client ID in `msal-config.js` (line 8)
- Backend issuer in `server.js` (line 21)
- Both should match

### Issue: Server not starting
**Check**: Port 3000 is not in use
**Fix**: 
```powershell
# Find process using port 3000
Get-Process -Id (Get-NetTCPConnection -LocalPort 3000).OwningProcess
# Kill it if needed, then restart
```

---

## 📝 Integration Summary

### What's Already Integrated:
✅ Primus Identity Validator SDK  
✅ Azure AD authentication  
✅ Protected API endpoints  
✅ Token validation  
✅ User information extraction  

### The Code:
```javascript
// 1. Import SDK (1 line)
const { primusIdentityMiddleware } = require('primus-identity-validator');

// 2. Configure (1 object)
const PRIMUS_CONFIG = {
    issuers: [{
        name: 'AzureAD',
        type: 'oidc',
        issuer: 'https://login.microsoftonline.com/YOUR-TENANT/v2.0',
        audiences: ['YOUR-CLIENT-ID']
    }]
};

// 3. Initialize (1 line)
const primusAuth = primusIdentityMiddleware(PRIMUS_CONFIG);

// 4. Protect endpoints (1 word!)
app.get('/api/revenue-stats', primusAuth, (req, res) => {
    // Only authenticated users reach here
    res.json({ user: req.primusUser });
});
```

**That's it! 🎉**

---

## 🎬 Demo Checklist

Before presenting:
- [ ] All 3 services running
- [ ] Browser tabs open (Portal, Dashboard)
- [ ] VS Code open with `server.js`
- [ ] Test login flow works
- [ ] Azure AD credentials ready
- [ ] Network tab ready to show

---

## 📞 Support

For detailed executive demo guide, see:
`C:\Users\aakib\.gemini\antigravity\brain\c20cec0b-3727-4603-84d2-f4bc96cc308f\executive_demo_guide.md`

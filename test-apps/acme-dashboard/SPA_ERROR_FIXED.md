# 🔧 Cross-Origin Error Fixed!

**Error**: `AADSTS9002326: Cross-origin token redemption is permitted only for the 'Single-Page Application' client-type.`

**Root Cause**: The redirect URIs were registered under the **Web** platform in Azure AD, but MSAL.js (running in browser) requires them to be under the **Single-Page Application (SPA)** platform.

**Solution**: Moved the redirect URIs from "Web" to "SPA" in the Azure AD app registration.

---

## ✅ Changes Applied

### **Azure AD Configuration**

**Updated App Registration**: `Acme Dashboard`

**Removed from Web**:
- `http://localhost:3000`
- `http://localhost:3000/auth/callback`

**Added to SPA**:
- `http://localhost:3000`
- `http://localhost:3000/auth/callback`

---

## 🚀 Try Again Now!

### **Step 1: Clear Browser Cache**

1. Close all tabs for `localhost:3000`
2. Clear cookies for localhost (important!)
3. Or use incognito/private window

### **Step 2: Login Again**

1. Go to: `http://localhost:3000`
2. Click **"Sign in with Microsoft"**
3. Login with: **akkhan2026@outlook.com**
4. Grant consent (if asked)
5. **Should work now!** ✅

---

## 📊 Why This Happened

1. **Azure CLI Default**: When creating an app via CLI, it defaults to "Web" platform.
2. **MSAL.js Requirement**: Modern MSAL.js (v2.0+) uses "Auth Code Flow with PKCE".
3. **Security Constraint**: Azure AD requires SPA platform registration for PKCE flow from browsers to prevent CORS errors.

**The configuration is now correct for a Single-Page Application.** 🚀

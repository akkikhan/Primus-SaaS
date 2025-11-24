# Release Process Example: Social Login Validation Feature

## 🎯 Scenario
- **New Feature**: Social login validation (Google, Facebook, GitHub, etc.)
- **Current Clients**: 5 applications using Identity Validator
- **Goal**: Release update, notify clients, maintain backward compatibility

---

## 📊 Step-by-Step Process

### Phase 1: Code & Documentation (Development)

#### Step 1: Implement the Feature
```javascript
// sdk/nodejs/primus-identity-validator/src/validator.ts
// Add social login validation logic
export async function validateSocialToken(token, provider) {
    // Your social login validation code
}
```

#### Step 2: Maintain Backward Compatibility
**Important**: Your existing clients should NOT break!

```javascript
// ✅ GOOD: Add new optional features
export interface PrimusIdentityOptions {
    issuers: IssuerConfig[];
    clockSkew?: number;
    socialProviders?: SocialProviderConfig[];  // NEW - Optional!
}

// ❌ BAD: Change existing required parameters
// This would break existing clients!
```

**Backward Compatibility Checklist**:
- ✅ New parameters are **optional** (have default values)
- ✅ Existing parameters unchanged
- ✅ Existing functions still work the same way
- ✅ Old configuration still valid

#### Step 3: Update Documentation
Create a changelog and release notes:

```markdown
## Version 1.2.0 - Social Login Support

### 🎉 New Features
- Added support for social login providers (Google, Facebook, GitHub)
- New `socialProviders` configuration option
- New `validateSocialToken()` function

### 🔄 Backward Compatibility
- All existing configurations continue to work
- No breaking changes
- Existing Azure AD and LocalAuth validation unchanged

### 📦 Installation
npm install @primus-saas/identity-validator@1.3.0

### 🚀 Migration Guide
**Existing clients**: No changes needed! Your code continues to work.

**To use social logins**: Add optional `socialProviders` configuration:
\`\`\`javascript
const PRIMUS_CONFIG = {
    issuers: [
        // Your existing issuers (Azure AD, LocalAuth) - unchanged
    ],
    socialProviders: [  // NEW - Optional!
        {
            name: 'Google',
            clientId: 'your-google-client-id',
            validateTokenUrl: 'https://oauth2.googleapis.com/tokeninfo'
        }
    ]
};
\`\`\`
```

#### Step 4: Update Version Numbers
Since this is a **new feature** (not a bug fix), use **MINOR** version bump:
- Current: `1.1.0`
- New: `1.2.0` (MAJOR.MINOR.PATCH)

---

### Phase 2: Portal Configuration (Your Portal UI)

#### Step 5: Log into Your Portal
Navigate to: `http://localhost:3000` (or your production portal URL)

#### Step 6: Register the New Version
Go to **Modules → Identity Validator → Versions**

Click **"Add New Version"** and fill in:

| Field | Value |
|-------|-------|
| **Version Number** | 1.2.0 |
| **Release Date** | 2025-11-23 |
| **Release Notes** | "Added support for social login providers (Google, Facebook, GitHub)" |
| **Changelog URL** | `https://github.com/akkikhan/Primus-SaaS/releases/tag/v1.2.0` |
| **Is Breaking Change** | ❌ No (backward compatible) |
| **NPM Package Name** | primus-identity-validator |
| **NuGet Package Name** | PrimusSaaS.Identity.Validator |
| **Status** | Draft |

**Save as Draft** for now.

#### Step 7: Create Package Registry Mappings (First Time Only)
If not already done, link your module to package registries:

Go to **Modules → Identity Validator → Package Mappings**

Add two mappings:
1. **NPM Mapping**:
   - Registry Type: `npm`
   - Package Name: `primus-identity-validator`
   - Registry URL: `https://www.npmjs.com/package/primus-identity-validator`

2. **NuGet Mapping**:
   - Registry Type: `nuget`
   - Package Name: `PrimusSaaS.Identity.Validator`
   - Registry URL: `https://www.nuget.org/packages/PrimusSaaS.Identity.Validator`

---

### Phase 3: Build & Test (Local Development)

#### Step 8: Test Locally
```powershell
# Test NPM package
cd "C:\Users\aakib\Primus SaaS\sdk\nodejs\primus-identity-validator"
npm test
npm run build

# Test NuGet package
cd "C:\Users\aakib\Primus SaaS\sdk\dotnet\PrimusSaaS.Identity.Validator"
dotnet test
dotnet build
```

#### Step 9: Test with Demo App
```powershell
# Update test app to use local package
cd "C:\Users\aakib\Primus SaaS\test-apps\acme-dashboard"

# Link local NPM package for testing
npm link ../../sdk/nodejs/primus-identity-validator

# Test social login feature
node server.js
```

Verify:
- ✅ Existing Azure AD authentication still works
- ✅ Existing LocalAuth authentication still works
- ✅ New social login validation works
- ✅ No breaking changes

---

### Phase 4: Publish to Registries (Going Live)

#### Step 10: Publish NPM Package
```powershell
cd "C:\Users\aakib\Primus SaaS\sdk\nodejs\primus-identity-validator"

# Update version
npm version minor    # 1.1.0 → 1.2.0

# Publish (automatically runs build & tests)
npm publish

# Create Git tag
git push --tags

# Verify published
npm view primus-identity-validator versions
# Should show: [ '1.0.0', '1.1.0', '1.2.0' ]
```

#### Step 11: Publish NuGet Package
```powershell
cd "C:\Users\aakib\Primus SaaS\sdk\dotnet\PrimusSaaS.Identity.Validator"

# Update version in .csproj file manually
# Change: <Version>1.1.0</Version>
# To:     <Version>1.2.0</Version>

# Build package
dotnet pack --configuration Release

# Publish to NuGet
dotnet nuget push "bin\Release\PrimusSaaS.Identity.Validator.1.2.0.nupkg" `
  --api-key YOUR_NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json

# Commit version change
git add PrimusSaaS.Identity.Validator.csproj
git commit -m "Release v1.2.0: Social login support"
git push
```

#### Step 12: Create GitHub Release
1. Go to: https://github.com/akkikhan/Primus-SaaS/releases
2. Click **"Draft a new release"**
3. Fill in:
   - **Tag**: `v1.2.0`
   - **Title**: `v1.2.0 - Social Login Support`
   - **Description**: Paste your changelog from Step 3
4. Click **"Publish release"**

---

### Phase 5: Portal Updates & Notifications (Automated)

#### Step 13: Webhook Triggers (Automatic)
Once you publish to NPM/NuGet, the registries automatically:

1. **NPM Registry** → Sends webhook to `https://your-portal.com/api/webhooks/npm-registry`
2. **NuGet Registry** → Sends webhook to `https://your-portal.com/api/webhooks/nuget-registry`

Your `WebhooksController` receives these webhooks and:
- ✅ Validates signature
- ✅ Parses package name and version
- ✅ Updates version status from "Draft" to "Published"
- ✅ Records publish timestamp

#### Step 14: User Notification (Automatic)
Your `EmailService` automatically:

1. **Queries database** for all applications using Identity Validator:
   ```sql
   SELECT a.* FROM Applications a
   JOIN ApplicationModules am ON a.Id = am.ApplicationId
   JOIN Modules m ON am.ModuleId = m.Id
   WHERE m.Name = 'Identity Validator'
   ```

2. **Checks user preferences** from `NotificationPreferences` table:
   - User opted in to notifications? ✅
   - User wants emails for new versions? ✅
   - User wants emails for breaking changes? (No, this is minor)

3. **Sends emails** to all 5 clients:

---

### 📧 What Your 5 Clients Receive

**Email sent to each client:**

```
From: Primus SaaS Team <notifications@primus-saas.com>
To: client@acme-corp.com
Subject: New version 1.2.0 published for module Identity Validator

Hello,

A new version 1.2.0 of the module Identity Validator has been published.

🎉 What's New:
• Social login validation support (Google, Facebook, GitHub)
• Enhanced token validation for OAuth providers
• New optional `socialProviders` configuration

🔄 Backward Compatibility:
✅ No breaking changes
✅ Your existing configuration continues to work
✅ Update is optional but recommended

Release notes: Added support for social login providers with backward-compatible configuration

Changelog: https://github.com/akkikhan/Primus-SaaS/releases/tag/v1.2.0

📦 Installation commands:

For Node.js/Express:
npm install primus-identity-validator@1.2.0

For .NET/ASP.NET Core:
Install-Package PrimusSaaS.Identity.Validator -Version 1.2.0

📖 Documentation:
https://primus-saas.com/docs/identity-validator/social-login

Need help upgrading? Reply to this email or visit our support portal.

Best regards,
Primus SaaS Team
```

---

### Phase 6: Client Portal Experience (What Clients See)

#### Step 15: Clients Log Into Their Portal
When your 5 clients log into your portal, they see:

**Dashboard → Notifications Badge**:
- 🔴 **1 new update available**

**Applications Page → Their App Card**:
```
┌─────────────────────────────────────┐
│ Acme Financial Dashboard            │
│ ⚠️ Update Available                  │
│                                     │
│ Modules:                            │
│ • Identity Validator 1.1.0 → 1.2.0 │
│   [View Changes] [Update Now]      │
└─────────────────────────────────────┘
```

#### Step 16: Client Views Update Details
Client clicks **"View Changes"** and sees:

**Version Comparison Modal**:
```
Identity Validator: 1.1.0 → 1.2.0

✨ What's New in 1.2.0:
• Social login validation support
• Google, Facebook, GitHub OAuth support
• New optional socialProviders configuration

⚠️ Breaking Changes: None
✅ Backward Compatible: Yes

📦 Installation:
npm install primus-identity-validator@1.2.0

📖 Migration Guide:
No changes required! To use social logins, add optional configuration:

const PRIMUS_CONFIG = {
    issuers: [/* existing */],
    socialProviders: [  // NEW - Optional
        { name: 'Google', clientId: 'xxx' }
    ]
};

[Close] [Update Now]
```

#### Step 17: Client Updates Their Code
Client has two options:

**Option A: Just Update Package (Backward Compatible)**
```powershell
npm install primus-identity-validator@1.2.0
# Done! No code changes needed
```

**Option B: Use New Social Login Feature**
```javascript
// Update configuration
const PRIMUS_CONFIG = {
    issuers: [
        {
            name: 'AzureAD',
            type: 'oidc',
            issuer: 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0',
            authority: 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0',
            audiences: ['acc675f1-e32f-40b9-a0c6-716066cc6890']
        },
        {
            name: 'LocalAuth',
            type: 'jwt',
            issuer: 'http://localhost:4000',
            secret: 'local-dev-secret-123',
            audiences: ['acc675f1-e32f-40b9-a0c6-716066cc6890']
        }
    ],
    socialProviders: [  // NEW!
        {
            name: 'Google',
            clientId: 'your-google-client-id.apps.googleusercontent.com',
            validateTokenUrl: 'https://oauth2.googleapis.com/tokeninfo'
        }
    ],
    clockSkew: 300
};
```

---

### Phase 7: Version Support & Maintenance

#### Step 18: Maintain Previous Versions (Backward Compatibility)
Your platform supports multiple versions simultaneously:

| Version | Status | Support Level |
|---------|--------|---------------|
| 1.2.0 | Latest | ✅ Full support |
| 1.1.0 | Stable | ✅ Bug fixes only |
| 1.0.0 | Legacy | ⚠️ Security fixes only |

**NPM supports this automatically**:
```bash
# Clients can install any version
npm install @primus-saas/identity-validator@1.0.0  # Still works
npm install @primus-saas/identity-validator@1.1.0  # Still works
npm install @primus-saas/identity-validator@1.2.0  # Still works
npm install @primus-saas/identity-validator@1.3.0  # Latest
npm install @primus-saas/identity-validator        # Gets latest (1.3.0)
```

**NuGet supports this automatically**:
```bash
Install-Package PrimusSaaS.Identity.Validator -Version 1.0.0
Install-Package PrimusSaaS.Identity.Validator -Version 1.1.0
Install-Package PrimusSaaS.Identity.Validator -Version 1.2.0
Install-Package PrimusSaaS.Identity.Validator  # Latest
```

#### Step 19: Portal Tracks Version Adoption
Your portal shows analytics:

**Admin Dashboard → Identity Validator**:
```
Version Distribution (5 active clients):
┌──────────┬─────────┬────────────┐
│ Version  │ Clients │ Percentage │
├──────────┼─────────┼────────────┤
│ 1.2.0    │    2    │    40%     │
│ 1.1.0    │    2    │    40%     │
│ 1.0.0    │    1    │    20%     │
└──────────┴─────────┴────────────┘

⚠️ 1 client on outdated version
📧 Send reminder email [Send Now]
```

---

## 🎯 Summary: Your Complete Workflow

### What You Do (Manual Steps):

1. ✅ **Code the feature** with backward compatibility
2. ✅ **Test thoroughly** (unit tests + integration tests)
3. ✅ **Update version numbers** (1.1.0 → 1.2.0)
4. ✅ **Write changelog** and release notes
5. ✅ **Publish to NPM**: `npm publish`
6. ✅ **Publish to NuGet**: `dotnet nuget push`
7. ✅ **Create GitHub release** with documentation
8. ✅ **Register version in portal** (via UI or API)

### What Happens Automatically:

1. ✅ **Webhook triggers** when you publish to NPM/NuGet
2. ✅ **Portal updates** version records
3. ✅ **Emails sent** to all 5 clients automatically
4. ✅ **Dashboard notifications** appear for clients
5. ✅ **Version history** tracked in database
6. ✅ **Analytics updated** (adoption rates, active versions)

### What Your Clients Do:

1. ✅ **Receive email** notification
2. ✅ **Review changes** in portal
3. ✅ **Update package** (`npm install` or `Install-Package`)
4. ✅ **(Optional) Add new features** to their code
5. ✅ **Test and deploy** their updated app

---

## 🔑 Key Concepts Explained

### Backward Compatibility (What You Asked About!)
Also called "**backward compatibility**" or "**non-breaking changes**":

- **Definition**: Old code works with new library version
- **How**: Make new features **optional** (not required)
- **Why**: Clients can update without breaking their apps

**Example from your code**:
```javascript
// OLD configuration (still works in v1.2.0!)
const PRIMUS_CONFIG = {
    issuers: [/* ... */],
    clockSkew: 300
};

// NEW configuration (optional upgrade)
const PRIMUS_CONFIG = {
    issuers: [/* ... */],
    socialProviders: [/* NEW */],  // Optional!
    clockSkew: 300
};
```

### Breaking Changes (What to Avoid!)
Changes that **break** existing client code:

❌ **Examples**:
- Renaming required parameters
- Removing functions
- Changing function signatures
- Changing required parameter types

When you **must** make breaking changes:
- Bump **MAJOR** version: 1.2.0 → 2.0.0
- Send **breaking change email** (different template)
- Provide **detailed migration guide**
- Give clients **advance notice** (e.g., 30 days)

---

## 📱 Portal UI Flow (Visual Guide)

### For You (Admin):
```
Portal Home → Modules → Identity Validator
    ↓
Click "Versions" tab
    ↓
Click "Add New Version"
    ↓
Fill form (v1.2.0, release notes, etc.)
    ↓
Save as "Draft"
    ↓
[Publish to NPM/NuGet via terminal]
    ↓
Webhook updates status to "Published"
    ↓
Email notifications sent automatically
```

### For Clients:
```
Login to Portal
    ↓
See notification badge (1 update)
    ↓
Go to "My Applications"
    ↓
See "Update Available" on their app card
    ↓
Click "View Changes"
    ↓
Read release notes & migration guide
    ↓
Copy installation command
    ↓
Update their code
```

---

## 🚀 Quick Reference Commands

### Publishing Checklist
```powershell
# 1. Update NPM package
cd sdk/nodejs/primus-identity-validator
npm version minor              # 1.1.0 → 1.2.0
npm publish
git push --tags

# 2. Update NuGet package
cd sdk/dotnet/PrimusSaaS.Identity.Validator
# Edit .csproj: <Version>1.2.0</Version>
dotnet pack --configuration Release
dotnet nuget push "bin\Release\*.nupkg" --api-key $KEY --source https://api.nuget.org/v3/index.json
git add .
git commit -m "Release v1.2.0"
git push

# 3. Create GitHub release
# Go to: https://github.com/akkikhan/Primus-SaaS/releases/new
# Tag: v1.2.0, paste changelog

# 4. Verify notifications sent
# Check portal admin dashboard
# Check email logs
```

---

## ❓ FAQ

**Q: What if a client doesn't want to update?**
A: That's fine! Backward compatibility means they can stay on 1.1.0 indefinitely. NPM/NuGet keeps all old versions available.

**Q: Can I force clients to update?**
A: No, but you can deprecate old versions and send reminders. For security issues, send urgent notifications.

**Q: How do I know which clients updated?**
A: Your portal tracks this! Check the "Version Distribution" analytics on the module page.

**Q: What if I need to make a breaking change?**
A: Bump MAJOR version (1.x.x → 2.0.0), send breaking change emails, and provide a detailed migration guide with at least 30 days notice.

**Q: Can I unpublish a version if there's a bug?**
A: NPM allows unpublishing within 72 hours. NuGet allows unlisting (hides but keeps available). Better: publish a patch version (1.2.0 → 1.2.1) to fix the bug.

---

Need help with any specific step? Just ask! 🚀

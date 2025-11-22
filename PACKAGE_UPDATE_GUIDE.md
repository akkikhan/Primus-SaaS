# Package Update & Notification Guide

## 🎯 Quick Overview

You have two SDK packages that developers integrate into their apps:
1. **NPM Package** (for Node.js/JavaScript): `primus-identity-validator`
2. **NuGet Package** (for .NET/C#): `PrimusSaaS.Identity.Validator`

When you update these packages, users need to know! This guide shows you how.

---

## 📦 Step 1: Publishing NPM Package Updates

### Current Status
- **Published Version**: 1.0.0 (live on npmjs.com)
- **Local Code Version**: 1.1.0 (not published yet)
- **Link**: https://www.npmjs.com/package/primus-identity-validator

### How to Publish an Update

```powershell
# Navigate to NPM package folder
cd "C:\Users\aakib\Primus SaaS\sdk\nodejs\primus-identity-validator"

# Step 1: Update the version number
npm version patch    # For bug fixes (1.1.0 → 1.1.1)
# OR
npm version minor    # For new features (1.1.0 → 1.2.0)
# OR
npm version major    # For breaking changes (1.1.0 → 2.0.0)

# Step 2: Build the package (happens automatically before publish)
npm run build

# Step 3: Run tests (happens automatically before publish)
npm test

# Step 4: Publish to NPM
npm publish

# Step 5: Push the version tag to GitHub
git push --tags
```

### What Happens Automatically
- `package.json` includes a `prepublishOnly` script that runs `build` and `test`
- NPM registry validates your credentials
- Package becomes available at `npm install primus-identity-validator@<version>`

---

## 📦 Step 2: Publishing NuGet Package Updates

### Current Status
- **Published Version**: None (never published)
- **Local Code Version**: 1.1.0 (ready to publish)
- **Future Link**: https://www.nuget.org/packages/PrimusSaaS.Identity.Validator

### First-Time Setup (One Time Only)
1. Go to https://www.nuget.org/account/apikeys
2. Create a new API key with "Push" permissions
3. Save it securely (you'll need it for publishing)

### How to Publish an Update

```powershell
# Navigate to .NET package folder
cd "C:\Users\aakib\Primus SaaS\sdk\dotnet\PrimusSaaS.Identity.Validator"

# Step 1: Update version in project file
# Edit PrimusSaaS.Identity.Validator.csproj and change:
# <Version>1.1.0</Version> to <Version>1.2.0</Version>

# Step 2: Build the package
dotnet pack --configuration Release

# Step 3: Publish to NuGet (replace YOUR_API_KEY with your actual key)
dotnet nuget push "bin\Release\PrimusSaaS.Identity.Validator.1.1.0.nupkg" `
  --api-key YOUR_API_KEY `
  --source https://api.nuget.org/v3/index.json

# Step 4: Commit and push the version change
git add PrimusSaaS.Identity.Validator.csproj
git commit -m "Bump NuGet package version to 1.1.0"
git push
```

---

## 🔔 Step 3: Setting Up User Notifications

### The Good News: It's Already Built! ✅

Your platform already has a notification system (added in commit 0b0edbb). Here's how it works:

### Notification Flow
```
Developer publishes update
        ↓
NPM/NuGet registry detects new version
        ↓
Registry triggers webhook to your portal
        ↓
WebhooksController validates & processes
        ↓
EmailService queries user preferences
        ↓
Emails sent to subscribed users
```

### What You Need to Do

#### A. Configure NPM Webhook (After First Publish)

1. **Log in to npmjs.com**
2. **Go to your package**: https://www.npmjs.com/package/primus-identity-validator
3. **Navigate to**: Settings → Webhooks
4. **Add webhook**:
   - **URL**: `https://your-portal-domain.com/api/webhooks/npm-registry`
   - **Secret**: Create a strong secret (save it for next step)
   - **Events**: Select "package:publish"

5. **Update your portal's configuration** (`appsettings.json`):
```json
{
  "Webhooks": {
    "NpmSecret": "your-strong-secret-here"
  }
}
```

#### B. Configure NuGet Webhook (After First Publish)

1. **Log in to nuget.org**
2. **Go to your package**: https://www.nuget.org/packages/PrimusSaaS.Identity.Validator
3. **Navigate to**: Manage Package → Webhooks
4. **Add webhook**:
   - **URL**: `https://your-portal-domain.com/api/webhooks/nuget-registry`
   - **Secret**: Create a strong secret (save it for next step)
   - **Events**: Select "PackagePushed"

5. **Update your portal's configuration** (`appsettings.json`):
```json
{
  "Webhooks": {
    "NuGetSecret": "your-strong-secret-here"
  }
}
```

#### C. Email Service Configuration

Your `EmailService` is already implemented. Just configure SMTP settings in `appsettings.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "notifications@primus-saas.com",
    "FromName": "Primus SaaS Team"
  }
}
```

**Note**: For Gmail, use an [App Password](https://support.google.com/accounts/answer/185833), not your regular password.

---

## 📧 What Users Receive

When you publish an update, subscribed users get an email like this:

```
Subject: New version 1.2.0 published for module Identity Validator

Hello,

A new version 1.2.0 of the module Identity Validator has been published.

Release notes: Enhanced multi-issuer token validation with JWKS support

Changelog: See https://github.com/akkikhan/Primus-SaaS/releases/tag/v1.2.0

Installation commands:
• npm: npm install primus-identity-validator@1.2.0
• NuGet: Install-Package PrimusSaaS.Identity.Validator -Version 1.2.0

Best regards,
Primus SaaS Team
```

---

## 👥 How Users Subscribe to Notifications

Users manage their notification preferences through your portal:

1. **API Endpoints** (already implemented in `NotificationsController`):
   - `GET /api/notifications/preferences` - View current settings
   - `POST /api/notifications/preferences` - Subscribe to updates
   - `PUT /api/notifications/preferences/{id}` - Update preferences
   - `DELETE /api/notifications/preferences/{id}` - Unsubscribe

2. **User Options**:
   - `EmailOnNewVersion`: Get notified on any version update
   - `EmailOnBreakingChange`: Get notified only on breaking changes
   - `AdditionalEmails`: CC other team members

3. **Database Table**: `NotificationPreferences`
   - Automatically created via migration `20251122163101_AddNotificationPreferences`

---

## 🚀 Complete Workflow Example

### Scenario: You fixed a bug and want to release v1.1.1

```powershell
# 1. Make your code changes
# 2. Test everything works

# 3. Update NPM package
cd "C:\Users\aakib\Primus SaaS\sdk\nodejs\primus-identity-validator"
npm version patch                    # Updates to 1.1.1
npm publish                          # Publishes to NPM
git push --tags                      # Syncs version tag to GitHub

# 4. Update NuGet package
cd "C:\Users\aakib\Primus SaaS\sdk\dotnet\PrimusSaaS.Identity.Validator"
# Edit .csproj: <Version>1.1.1</Version>
dotnet pack --configuration Release
dotnet nuget push "bin\Release\PrimusSaaS.Identity.Validator.1.1.1.nupkg" --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json
git add PrimusSaaS.Identity.Validator.csproj
git commit -m "Bump to v1.1.1"
git push

# 5. Notifications happen automatically!
# - NPM registry triggers webhook
# - NuGet registry triggers webhook
# - Portal processes both webhooks
# - Emails sent to all subscribed users
```

---

## 🔧 Testing the Notification System

Before going live, test the webhook flow:

```powershell
# Test NPM webhook locally
cd "C:\Users\aakib\Primus SaaS\test-apps"
.\test-webhook.ps1 -WebhookType npm -PackageName "primus-identity-validator" -Version "1.1.1"

# Test NuGet webhook locally
.\test-webhook.ps1 -WebhookType nuget -PackageName "PrimusSaaS.Identity.Validator" -Version "1.1.1"
```

Check your portal logs to see:
- ✅ Webhook signature validation
- ✅ Payload parsing
- ✅ User preference queries
- ✅ Email delivery attempts

---

## 📋 Checklist: First Time Publishing

### NPM Package
- [ ] Code is at version 1.1.0 locally
- [ ] Tests pass (`npm test`)
- [ ] Build succeeds (`npm run build`)
- [ ] NPM account has publish permissions
- [ ] Run `npm publish`
- [ ] Configure webhook on npmjs.com
- [ ] Add webhook secret to `appsettings.json`

### NuGet Package  
- [ ] Code is at version 1.1.0 in `.csproj`
- [ ] Create NuGet API key
- [ ] Build succeeds (`dotnet pack`)
- [ ] Run `dotnet nuget push`
- [ ] Configure webhook on nuget.org
- [ ] Add webhook secret to `appsettings.json`

### Notification System
- [ ] SMTP settings configured in `appsettings.json`
- [ ] Email service tested (send test email)
- [ ] Portal deployed with webhook endpoints accessible
- [ ] Webhook URLs are HTTPS (required by registries)
- [ ] Test webhook with mock payload

---

## 🆘 Troubleshooting

### "npm publish" fails with 401 Unauthorized
```powershell
# Log in to NPM
npm login
# Enter your username, password, and email
```

### "dotnet nuget push" fails with 403 Forbidden
- Check your API key has "Push" permissions
- Verify the API key hasn't expired
- Ensure you're using the correct source URL

### Webhooks not triggering
- Verify webhook URL is publicly accessible (use ngrok for local testing)
- Check webhook secret matches in both registry settings and `appsettings.json`
- Look at webhook delivery logs in NPM/NuGet registry dashboard

### Emails not sending
- Test SMTP credentials: `telnet smtp.gmail.com 587`
- Check spam folder
- Review portal logs for email service errors
- Verify `EmailSettings` in `appsettings.json`

---

## 📚 Additional Resources

- **NPM Publishing Docs**: https://docs.npmjs.com/cli/v9/commands/npm-publish
- **NuGet Publishing Docs**: https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package
- **Semantic Versioning**: https://semver.org/
- **Your Package Links**:
  - NPM: https://www.npmjs.com/package/primus-identity-validator
  - GitHub: https://github.com/akkikhan/Primus-SaaS
  - NuGet: https://www.nuget.org/packages/PrimusSaaS.Identity.Validator (after first publish)

---

## 🎓 Key Concepts

### Version Numbers (Semantic Versioning)
- **MAJOR** (1.x.x): Breaking changes - users must update their code
- **MINOR** (x.1.x): New features - backward compatible
- **PATCH** (x.x.1): Bug fixes - backward compatible

### Your Current Status
| Package | Published | Local Code | Action Needed |
|---------|-----------|------------|---------------|
| NPM | 1.0.0 | 1.1.0 | Publish v1.1.0 |
| NuGet | None | 1.1.0 | First publish |

---

**Need more help?** Just ask! I can walk you through any specific step.

# Manual Publishing Instructions

The API key provided appears to be invalid or expired. Here are alternative methods to publish your packages:

---

## Option 1: Get a New API Key from NuGet.org

1. Go to https://www.nuget.org/account/apikeys
2. Click "Create" to generate a new API key
3. Set the following:
   - **Key Name**: PrimusSaaS Packages
   - **Glob Pattern**: PrimusSaaS.*
   - **Expiration**: 365 days (or your preference)
   - **Scopes**: Push new packages and package versions

4. Copy the generated API key

5. Run these commands with your new key:

```powershell
# Set your new API key
$apiKey = "YOUR_NEW_API_KEY"

# Publish Identity.Validator 1.2.1
cd "c:\Users\aakib\Primus SaaS\sdk\dotnet\PrimusSaaS.Identity.Validator"
dotnet nuget push bin/Release/PrimusSaaS.Identity.Validator.1.2.1.nupkg --api-key $apiKey --source https://api.nuget.org/v3/index.json

# Publish Logging 1.1.0
cd "c:\Users\aakib\Primus SaaS\sdk\logging\dotnet\PrimusSaaS.Logging"
dotnet nuget push bin/Release/PrimusSaaS.Logging.1.1.0.nupkg --api-key $apiKey --source https://api.nuget.org/v3/index.json
```

---

## Option 2: Upload via NuGet.org Web Interface

This is the easiest method if you prefer not to use the command line:

1. **Go to NuGet.org Upload Page:**
   - Visit: https://www.nuget.org/packages/manage/upload

2. **Upload Identity.Validator 1.2.1:**
   - Click "Browse" or drag and drop
   - Select: `c:\Users\aakib\Primus SaaS\sdk\dotnet\PrimusSaaS.Identity.Validator\bin\Release\PrimusSaaS.Identity.Validator.1.2.1.nupkg`
   - Click "Upload"
   - Review package details
   - Click "Submit"

3. **Upload Logging 1.1.0:**
   - Click "Browse" or drag and drop
   - Select: `c:\Users\aakib\Primus SaaS\sdk\logging\dotnet\PrimusSaaS.Logging\bin\Release\PrimusSaaS.Logging.1.1.0.nupkg`
   - Click "Upload"
   - Review package details
   - Click "Submit"

4. **Wait for Validation:**
   - Packages typically appear within 5-10 minutes
   - You'll receive an email confirmation

---

## Package Locations

### Identity.Validator 1.2.1
```
c:\Users\aakib\Primus SaaS\sdk\dotnet\PrimusSaaS.Identity.Validator\bin\Release\PrimusSaaS.Identity.Validator.1.2.1.nupkg
```

### Logging 1.1.0
```
c:\Users\aakib\Primus SaaS\sdk\logging\dotnet\PrimusSaaS.Logging\bin\Release\PrimusSaaS.Logging.1.1.0.nupkg
```

---

## After Publishing

### Verify Packages Are Live

1. **Check package pages:**
   - https://www.nuget.org/packages/PrimusSaaS.Identity.Validator/1.2.1
   - https://www.nuget.org/packages/PrimusSaaS.Logging/1.1.0

2. **Test installation:**
   ```bash
   dotnet new console -n TestInstall
   cd TestInstall
   dotnet add package PrimusSaaS.Identity.Validator --version 1.2.1
   dotnet add package PrimusSaaS.Logging --version 1.1.0
   dotnet build
   ```

3. **Verify documentation:**
   - Click "Documentation" tab on NuGet page
   - Verify all guides are visible

---

## Deploy Documentation Site

Once packages are published, deploy the documentation:

```bash
cd "c:\Users\aakib\Primus SaaS\docs-site"
npm install
npm run build
npm run deploy
```

Verify deployment at:
- https://akkikhan.github.io/docs/modules/identity-tenant-resolver
- https://akkikhan.github.io/docs/modules/logging-middleware
- https://akkikhan.github.io/docs/release-notes

---

## Notify Clients

After both packages are live and docs are deployed, send the notification email (template in PUBLISHING_GUIDE.md).

---

**Recommendation:** Use Option 2 (Web Interface) - it's the quickest and most reliable method!

# NuGet Package Publishing Guide

## Quick Start - 3 Steps to Publish

### Step 1: Get Your NuGet API Key

1. Go to **https://www.nuget.org/account/apikeys**
2. Sign in (or create an account if you don't have one)
3. Click **"Create"** to generate a new API key with these settings:
   - **Key Name**: `PrimusSaaS.Identity.Validator Publishing`
   - **Select Scopes**: ✅ Push new packages and package versions
   - **Glob Pattern**: `PrimusSaaS.Identity.Validator`
   - **Expires In**: 365 days (or your preference)
4. Click **"Create"**
5. **Copy the API key** immediately (you won't be able to see it again!)

### Step 2: Set Environment Variable

Open PowerShell in this directory and run:

```powershell
$env:NUGET_API_KEY = "your-api-key-here"
```

Replace `your-api-key-here` with the actual API key you copied.

### Step 3: Run the Publishing Script

```powershell
.\publish-nuget.ps1
```

The script will:
- ✅ Build the package in Release mode
- ✅ Create the .nupkg file
- ✅ Ask for confirmation
- ✅ Publish to NuGet.org
- ✅ Show you the next steps

## Manual Publishing (Alternative)

If you prefer to publish manually:

```powershell
# Navigate to the project directory
cd "c:\Users\aakib\Primus SaaS\sdk\dotnet\PrimusSaaS.Identity.Validator"

# Build and pack
dotnet pack --configuration Release

# Publish (replace YOUR_API_KEY with your actual key)
dotnet nuget push "bin\Release\PrimusSaaS.Identity.Validator.1.0.0.nupkg" `
  --api-key YOUR_API_KEY `
  --source https://api.nuget.org/v3/index.json
```

## After Publishing

1. **Wait 5-10 minutes** for NuGet.org to index your package
2. **Verify publication** at: https://www.nuget.org/packages/PrimusSaaS.Identity.Validator
3. **Test installation** in a test project:
   ```bash
   dotnet add package PrimusSaaS.Identity.Validator
   ```

## Troubleshooting

### "Package already exists"
If you see this error, the package version is already published. You need to:
- Increment the version in `PrimusSaaS.Identity.Validator.csproj`
- Rebuild and republish

### "Unauthorized"
Your API key is invalid or expired. Create a new one and try again.

### "Invalid package"
Check that:
- The package builds successfully: `dotnet build --configuration Release`
- All required metadata is in the .csproj file
- README.md exists in the project

## Package Information

- **Package Name**: PrimusSaaS.Identity.Validator
- **Current Version**: 1.0.0
- **Target Framework**: .NET 7.0
- **License**: MIT
- **Repository**: https://github.com/akkikhan/Primus-SaaS

## Security Note

⚠️ **Never commit your API key to Git!** The `$env:NUGET_API_KEY` is temporary and only lasts for your current PowerShell session.

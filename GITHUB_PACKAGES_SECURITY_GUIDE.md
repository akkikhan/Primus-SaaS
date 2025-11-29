# GitHub Packages Security Guide

## Is GitHub Packages Secure for Private Distribution?

**Yes**, when configured correctly. Here's how to maintain security:

## 1. Token Security (Critical)

### Create Minimal-Scope PATs
```bash
# For publishing (maintainers only):
Scopes: write:packages, read:packages

# For consuming (customers/users):
Scopes: read:packages only

# Add 'repo' scope ONLY if packages are in private repos
```

### Never Commit Tokens
- ❌ Don't use `--store-password-in-clear-text` in production
- ✅ Use environment variables or secure credential stores

### Rotate Tokens Regularly
- Set expiration dates (30-90 days)
- Revoke compromised tokens immediately
- Use fine-grained PATs when possible

## 2. Secure Configuration

### For CI/CD (GitHub Actions)
```yaml
- name: Publish to GitHub Packages
  run: dotnet nuget push nupkg/*.nupkg --source GitHub
  env:
    NUGET_AUTH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### For Local Development (nuget.config)
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="GitHub" value="https://nuget.pkg.github.com/akkikhan/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <GitHub>
      <add key="Username" value="akkikhan" />
      <add key="ClearTextPassword" value="%NUGET_AUTH_TOKEN%" />
    </GitHub>
  </packageSourceCredentials>
</configuration>
```

Then set environment variable:
```powershell
$env:NUGET_AUTH_TOKEN = "ghp_your_token_here"
```

## 3. Package Integrity

### Sign Your Packages
```bash
# Create a code signing certificate
dotnet nuget sign nupkg/*.nupkg \
  --certificate-path cert.pfx \
  --timestamper http://timestamp.digicert.com
```

### Verify Package Ownership
- Use GitHub's package namespace under your org
- Enable required reviewers for package publishing
- Monitor package versions in GitHub's Packages UI

## 4. Access Control

### Repository Settings
- Make packages private if they contain proprietary code
- Use GitHub Organizations for team-based access
- Enable 2FA for all team members with write access

### Audit Trail
- GitHub logs all package downloads with timestamps
- You can see which accounts/PATs pulled packages
- Review regularly in Packages → Insights

## 5. Consumer Security

### What Customers Should Do
```bash
# Pin specific versions (don't use wildcards)
dotnet add package PrimusSaaS.Identity.Validator --version 1.3.1

# Verify package signatures (if you sign)
dotnet nuget verify nupkg/*.nupkg
```

### What You Should Document
- Minimum required token scopes
- How to rotate tokens safely
- Support contact for security issues
- Package deprecation/vulnerability policy

## 6. Privacy Guarantees

✅ **What GitHub Packages Provides:**
- You see which GitHub accounts downloaded (aggregate)
- Consumers don't see other consumers
- No IPs or personal data exposed
- GDPR/SOC2 compliant infrastructure

❌ **What It Doesn't Do:**
- Expose consumer identities to package content
- Track usage after package is installed
- Share analytics with third parties

## 7. Alternative: Add Opt-In Telemetry (Optional)

If you need usage metrics beyond downloads, add **transparent, opt-in** telemetry:

```csharp
public class PrimusConfiguration
{
    /// <summary>
    /// Enable anonymous usage telemetry (opt-in).
    /// Sends library version and .NET runtime to improve support.
    /// No personal data collected. See: https://your-docs/privacy
    /// </summary>
    public bool EnableTelemetry { get; set; } = false;
}
```

Document clearly in README and honor opt-out.

## Quick Setup for Your Packages

### 1. Create Read/Write PAT
- Go to https://github.com/settings/tokens/new
- Name: "NuGet Publish"
- Scopes: `write:packages`, `read:packages`
- Expiration: 90 days
- Save token securely

### 2. Configure GitHub Actions
Add to `.github/workflows/publish-nuget.yml`:
```yaml
name: Publish NuGet Packages

on:
  release:
    types: [published]

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '6.0.x'
      
      - name: Pack
        run: |
          dotnet pack sdk/dotnet/Primus.Identity.Validator/Primus.Identity.Validator.csproj -c Release -o nupkg
          dotnet pack sdk/dotnet/Primus.Logging/Primus.Logging.csproj -c Release -o nupkg
      
      - name: Publish to GitHub Packages
        run: dotnet nuget push nupkg/*.nupkg --source https://nuget.pkg.github.com/akkikhan/index.json --api-key ${{ secrets.GITHUB_TOKEN }} --skip-duplicate
```

### 3. Consumer Instructions
Share with customers:

```powershell
# One-time setup
$env:NUGET_AUTH_TOKEN = "ghp_YOUR_READ_TOKEN"

dotnet nuget add source https://nuget.pkg.github.com/akkikhan/index.json `
  -n GitHub `
  -u akkikhan `
  -p %NUGET_AUTH_TOKEN%

# Install packages
dotnet add package PrimusSaaS.Identity.Validator --version 1.3.1
dotnet add package PrimusSaaS.Logging --version 1.2.2
```

## Recommendation

For **Primus SaaS**, I suggest:

1. **Public packages on nuget.org** (current setup): Keep for open-source/community adoption
2. **Private packages on GitHub Packages**: For enterprise customers or pre-release versions
3. **Signed packages**: Add code signing for both feeds
4. **Opt-in telemetry**: Add transparent flag for usage analytics (with clear privacy policy)

This gives you:
- ✅ Wide reach (nuget.org)
- ✅ Controlled distribution (GitHub Packages)
- ✅ Download tracking (GitHub analytics)
- ✅ Customer security (scoped tokens, signing)
- ✅ Privacy compliance (no hidden tracking)

## Security Checklist

- [ ] Created minimal-scope PATs
- [ ] Set token expiration dates
- [ ] Configured nuget.config with environment variables
- [ ] Added GitHub Actions workflow with secrets
- [ ] Enabled 2FA for GitHub account
- [ ] Documented consumer setup in README
- [ ] Added package signing (optional but recommended)
- [ ] Reviewed GitHub package permissions
- [ ] Added privacy policy for any telemetry
- [ ] Set up token rotation reminders

## Support

For questions about package security or access issues:
- GitHub: https://github.com/akkikhan/Primus-SaaS/issues
- Docs: [Add your documentation site URL]

# Primus SaaS Security Audit Report

**Date:** $(Get-Date -Format "yyyy-MM-dd")  
**Auditor:** AI Security Scanner  
**Repository:** akkikhan/Primus-SaaS  
**Branch:** AG-15  

---

## Executive Summary

This comprehensive security audit identified **4 CRITICAL**, **3 HIGH**, **4 MEDIUM**, and **2 LOW** severity issues across the Primus SaaS codebase. The most urgent issues involve **real credentials committed to the repository** that require immediate rotation.

| Severity | Count | Immediate Action Required |
|----------|-------|--------------------------|
| 🔴 CRITICAL | 4 | YES - Rotate credentials NOW |
| 🟠 HIGH | 3 | YES - Fix before next release |
| 🟡 MEDIUM | 4 | Recommended - Schedule fix |
| 🟢 LOW | 2 | Best practice improvements |

---

## 🔴 CRITICAL FINDINGS (Immediate Action Required)

### CRITICAL-1: Real Client Credentials in `acme-credentials.json`

**File:** `acme-credentials.json`  
**Risk:** Complete compromise of client authentication

```json
{
  "clientId": "PSP-CLI-711224",
  "clientSecret": "psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI"
}
```

**Impact:** Anyone with repository access can impersonate this client.

**Remediation:**
1. ⚡ **IMMEDIATELY** rotate this client secret in your auth system
2. Delete `acme-credentials.json` from the repository
3. Add to `.gitignore`: `*-credentials.json`, `*.credentials.json`
4. Use BFG Repo-Cleaner to purge from git history:
   ```bash
   bfg --delete-files acme-credentials.json
   git reflog expire --expire=now --all && git gc --prune=now --aggressive
   ```

---

### CRITICAL-2: Gmail App Password Exposed in `send_test_email.py`

**File:** `send_test_email.py` (lines 7-10)

```python
SMTP_HOST = "smtp.gmail.com"
SMTP_PORT = 587
SMTP_USER = "khanakkijpr@gmail.com"
SMTP_PASS = "fgoteyivfyylyfnk"  # ⚠️ REAL APP PASSWORD
```

**Impact:** Unauthorized email sending, potential phishing attacks using your email.

**Remediation:**
1. ⚡ **IMMEDIATELY** revoke this App Password at https://myaccount.google.com/apppasswords
2. Delete or sanitize `send_test_email.py`
3. Use environment variables: `os.environ.get('SMTP_PASS')`
4. Purge from git history with BFG

---

### CRITICAL-3: JWT Tokens in Repository

**Files:** `token.json`, `token_final.json`

**Impact:** These tokens may still be valid and could grant unauthorized access.

**Remediation:**
1. ⚡ **IMMEDIATELY** check token expiration and invalidate if still valid
2. Files are already in `.gitignore` (added this session) ✅
3. Delete the files: `Remove-Item token.json, token_final.json -Force`
4. Purge from git history with BFG

---

### CRITICAL-4: PSP Client Secret in Documentation

**File:** `GAP_ANALYSIS_AZURE_AD.md` (lines 117-120)

```
Client ID: PSP-CLI-711224
Client Secret: psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI
```

**Impact:** Duplicate exposure - same credential as CRITICAL-1.

**Remediation:**
1. Remove secret from documentation
2. Replace with placeholder: `Client Secret: <rotated - contact admin>`

---

## 🟠 HIGH SEVERITY FINDINGS

### HIGH-1: Default Passwords in 46+ Locations

**Pattern:** `Admin123!` found across codebase

**Key Locations:**
| File | Line | Context |
|------|------|---------|
| `portal/frontend/src/pages/LoginPage.tsx` | 14 | Default form value |
| `portal/backend/Program.cs` | 54 | Seed user password |
| `docker-compose.yml` | 31 | JWT secret fallback |
| `examples/LiveDemoApi/appsettings.json` | 51 | Local JWT secret |
| `PROGRESS.md` | 414 | Documentation |

**Impact:** Predictable credentials enable unauthorized access in deployed environments.

**Remediation:**
1. Remove default password from `LoginPage.tsx` form fields
2. Add environment variable requirement for seed user passwords
3. Add startup validation that rejects default passwords in Production:
   ```csharp
   if (env.IsProduction() && jwtKey == "your-super-secret-jwt-key-change-this-in-production")
       throw new InvalidOperationException("Default JWT key not allowed in Production!");
   ```

---

### HIGH-2: npm Vulnerability - node-forge (docs-site)

**Package:** `node-forge <=1.3.1`  
**Severity:** HIGH  
**CVE:** Prototype Pollution vulnerability

**Remediation:**
```bash
cd docs-site
npm audit fix --force
# Or update package-lock.json manually
```

---

### HIGH-3: docker-compose Default Password

**File:** `docker-compose.yml`

```yaml
SA_PASSWORD: ${DB_PASSWORD:-YourStrong@Passw0rd}
Jwt__Key: ${JWT_SECRET:-your-super-secret-jwt-key-change-this-in-production}
```

**Impact:** If environment variables aren't set, insecure defaults are used.

**Remediation:**
1. Add startup check that fails if default values are detected
2. Update DOCKER_SETUP.md with mandatory `.env` file creation
3. Consider removing defaults entirely (fail-fast):
   ```yaml
   SA_PASSWORD: ${DB_PASSWORD:?DB_PASSWORD environment variable required}
   ```

---

## 🟡 MEDIUM SEVERITY FINDINGS

### MEDIUM-1: npm Vulnerabilities - esbuild/vite (portal/frontend)

**Packages:** `esbuild`, `vite <=6.1.6`  
**Severity:** MODERATE  
**Count:** 2 vulnerabilities

**Remediation:**
```bash
cd portal/frontend
npm update vite esbuild
npm audit fix
```

---

### MEDIUM-2: npm Vulnerability - mdast-util-to-hast (docs-site)

**Package:** `mdast-util-to-hast`  
**Severity:** MODERATE

**Remediation:**
```bash
cd docs-site
npm audit fix
```

---

### MEDIUM-3: Starter Kit .zip Files Include Build Artifacts

**Files:** `docs-site/static/downloads/*.zip`

**Issue:** Some .zip files include `bin/` and `obj/` folders which:
- Bloat download size unnecessarily
- May contain environment-specific build outputs
- Could theoretically contain cached credentials

**Remediation:**
1. Rebuild .zip files excluding build artifacts:
   ```powershell
   Compress-Archive -Path src/*, *.csproj, appsettings.json -DestinationPath starter.zip
   ```
2. Add pre-publish script to clean directories

---

### MEDIUM-4: Missing Security Headers Validation

**Observation:** No explicit security header middleware found.

**Recommended Headers:**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
```

---

## 🟢 LOW SEVERITY FINDINGS

### LOW-1: .NET SDK Version Mismatch

**Issue:** Some projects target net6.0-net9.0 but vulnerability scanning requires .NET 8+ SDK installed.

**Recommendation:** Update development environment to .NET 8 SDK for full vulnerability scanning.

---

### LOW-2: Inconsistent Password Complexity Enforcement

**Observation:** While BCrypt is properly used for password hashing, there's no visible password complexity validation in the registration flow.

**Recommendation:** Add password strength validation:
```csharp
public static class PasswordValidator
{
    public static bool IsStrong(string password) =>
        password.Length >= 12 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit) &&
        password.Any(c => !char.IsLetterOrDigit(c));
}
```

---

## ✅ SECURITY POSITIVES

The audit also identified several security best practices already in place:

| Area | Status | Notes |
|------|--------|-------|
| JWT Validation | ✅ Excellent | Multi-issuer support, proper signature validation, clock skew handling |
| Password Hashing | ✅ BCrypt | Portal uses BCrypt.Net.BCrypt (industry standard) |
| CI/CD Secrets | ✅ Secure | All workflows use `${{ secrets.* }}` properly |
| Rate Limiting | ✅ Implemented | FailedValidationRateLimiter protects against brute force |
| HTTPS Enforcement | ✅ Configurable | `RequireHttpsMetadata` option available |
| PII Logging Protection | ✅ Implemented | IdentityLogHelper hashes sensitive data before logging |
| Token Expiration | ✅ Required | `RequireExpirationTime = true` enforced |
| Algorithm Pinning | ✅ Secure | `RequireSignedTokens = true`, no algorithm confusion |

---

## Remediation Priority Matrix

| Priority | Action | Effort | Impact |
|----------|--------|--------|--------|
| P0 | Rotate `psp_` client secret | 5 min | Prevents credential abuse |
| P0 | Revoke Gmail App Password | 2 min | Prevents email abuse |
| P0 | Delete token.json files | 1 min | Already in .gitignore |
| P1 | Remove default passwords from code | 1 hour | Prevents weak auth |
| P1 | Run `npm audit fix` in 2 projects | 10 min | Patches known CVEs |
| P2 | Add Production startup validation | 30 min | Fail-fast for misconfig |
| P2 | Rebuild .zip files without bin/obj | 15 min | Cleaner downloads |
| P3 | Add security headers middleware | 20 min | Defense in depth |

---

## Files to Add to .gitignore

Already added this session:
```gitignore
token.json
token_final.json
temp-security-scan/
SECURITY_SCAN_RESULTS.md
```

**Recommended additions:**
```gitignore
# Credential files
*-credentials.json
*.credentials.json
*.secrets.json
secrets/

# Email test scripts with credentials
send_test_email.py

# Local environment overrides
.env.local
.env.*.local
```

---

## Git History Cleanup Commands

To fully remove sensitive files from git history:

```bash
# Install BFG Repo-Cleaner
# https://rtyley.github.io/bfg-repo-cleaner/

# Remove specific files from history
bfg --delete-files acme-credentials.json
bfg --delete-files send_test_email.py
bfg --delete-files token.json
bfg --delete-files token_final.json

# Cleanup
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# Force push (coordinate with team!)
git push --force --all
```

---

## Conclusion

The Primus SaaS codebase demonstrates **strong security fundamentals** in authentication, password hashing, and CI/CD practices. However, **4 critical credential exposures require immediate action** before any public distribution of the code.

### Immediate Actions Required:
1. 🔴 Rotate the PSP client secret
2. 🔴 Revoke the Gmail App Password
3. 🔴 Delete/purge credential files from history
4. 🟠 Remove default password patterns from code

After completing these actions, the repository will be safe for:
- ✅ NuGet/npm package publishing
- ✅ GitHub public visibility
- ✅ Client SDK distribution
- ✅ Documentation site deployment

---

*Report generated by automated security analysis. Manual review recommended for business logic vulnerabilities.*

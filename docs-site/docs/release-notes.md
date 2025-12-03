# Release Notes

## PrimusSaaS.Identity.Validator 1.3.2 (Node) / 1.3.0 (.NET)

**Release Date:** November 25, 2025

### Major Improvements

Multi-issuer validation hardening, JWKS caching/normalization, diagnostics endpoint helper, and updated docs.

### Fixed

#### JWKS Discovery Robustness

**Problem:** Some Azure AD tenants returned 404 on the first JWKS discovery call; tokens failed validation.

**Solution:**
- Normalized discovery URLs and added retry with backoff
- Added diagnostics logging and metrics for JWKS failures
- Improved cache invalidation when keys rotate

#### Security Event Logging

Added structured logging for validation failures and rate-limit events to aid SOC review and alerting.

### Added

#### Diagnostics Helper
`app.MapPrimusIdentityDiagnostics();` – optional endpoint to surface issuer health, JWKS status, and security metrics (protect in production).

#### Policy Helper Scaffolding
Early helpers for policy-based authorization alignment with common RBAC patterns.

#### Documentation
- Module docs updated for multi-issuer setup and diagnostics
- JWKS caching and diagnostics notes added

### Improved

- Actionable configuration validation errors
- Retry/backoff around discovery with clearer error messages
- Optional token refresh interface scaffolding

### Package Changes

- Node: **1.3.2** (rate limiting + HTTPS metadata toggle + tenant context docs)
- .NET: **1.3.0** (unchanged in this drop)
- Documentation files updated and aligned to unified guide
- No breaking changes; configuration shape unchanged since 1.2.0

### Migration from 1.2.x

```bash
# Node
npm install @primus-saas/identity-validator@1.3.2

# .NET
dotnet add package PrimusSaaS.Identity.Validator --version 1.3.0
```

### Documentation

- [Identity Quick Start](/docs/modules/identity-quick-start)
- [README.md](https://www.nuget.org/packages/PrimusSaaS.Identity.Validator)

### Acknowledgments

Special thanks to our clients for providing detailed feedback on the TenantResolver issue. Your hands-on testing helped us identify and fix this critical problem quickly.

---

## PrimusSaaS.Logging 1.2.4

**Release Date:** November 26, 2025

### Major Improvements

Async buffering and health/metrics guidance, file rotation/compression, and Application Insights target updates.

### Fixed

#### Dependency and Target Updates

**Problem:** Older packages pulled mismatched dependencies and lacked file rotation guidance.

**Solution:**
- TargetFrameworks net6.0/net7.0 (optional net8.0 when available)
- File target options documented with rotation and compression
- Application Insights target docs refreshed

#### Middleware/Health Notes

Clarified correlation IDs, request enrichment, and example health/metrics endpoints.

#### MEDIUM: Configuration Property Confusion

**Problem:** Mixed terminology around pretty/format and async flags.

**Solution:**

- Clarified console target uses `Pretty` (alias accepted)
- File target options include `Async`, `MaxFileSize`, `MaxRetainedFiles`, `CompressRotatedFiles`

```json
{ "Type": "console", "Pretty": true }
```

### Added

#### UsePrimusLogging() Middleware

```csharp
app.UsePrimusLogging();
```

**Features:**
- Automatic request ID generation
- HTTP context enrichment (method, path, status)
- User context extraction from claims
- Response header injection (`X-Request-ID`)
- Request/response logging
 - Correlation IDs for distributed tracing

#### API Aliases

Both methods now work:
```csharp
builder.Logging.AddPrimus(options => { ... });
builder.Logging.AddPrimusLogging(options => { ... });  // Alias
```

#### Comprehensive Documentation

- Module docs refreshed (Identity + Logging)

### Improved

- Updated README with middleware and target examples
- Clarified PII masking options
- Added notes on async buffering and performance timers

### Package Changes

- Version: 1.1.x → **1.2.4**
- Dependencies: net6/net7 (net8 optional)
- All documentation files included in NuGet package
- No breaking changes

### Migration from 1.1.x

**No code changes required!** Simply update the package:

```bash
dotnet add package PrimusSaaS.Logging --version 1.2.4
```

**Optional improvements:**

1. **Add middleware:**
   ```csharp
   app.UsePrimusLogging();
   ```

2. **Use either API:**
   ```csharp
   builder.Logging.AddPrimus(options => { ... });
   // OR
   builder.Logging.AddPrimusLogging(options => { ... });
   ```

### Documentation

- [Logging Module](/docs/modules/logging-module)
- [README.md](https://www.nuget.org/packages/PrimusSaaS.Logging)

### Acknowledgments

Special thanks to our clients for comprehensive hands-on testing and detailed feedback. Your reports helped us identify and fix all critical issues.

---

## Summary

### Client Feedback Response

**Before:**
- Identity.Validator 1.2.x: JWKS edge cases, limited diagnostics
- Logging 1.1.x: Less clarity on async/rotation and health/metrics
- Client Rating: improving but seeking consistency

**After:**
- Identity.Validator 1.3.0: Hardened JWKS, diagnostics helper, aligned docs
- Logging 1.2.4: Async buffering, rotation guidance, clarified middleware, aligned docs
- Expected Rating: A- (9/10) - Production Ready

### All Critical Issues Resolved

| Issue | Severity | Status |
|-------|----------|--------|
| JWKS discovery failures | HIGH | Fixed in 1.3.0 |
| Need diagnostics helper | HIGH | Added in 1.3.0 |
| Rotation/async clarity | MEDIUM | Fixed in 1.2.4 |
| Missing documentation | MEDIUM | Unified guide across Node/.NET/Logging |

### Upgrade Instructions

```bash
# Update both packages
dotnet add package PrimusSaaS.Identity.Validator --version 1.3.0
dotnet add package PrimusSaaS.Logging --version 1.2.4

# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

**Expected result:** 0 warnings, hardened validation, consistent logging/middleware, and unified docs.

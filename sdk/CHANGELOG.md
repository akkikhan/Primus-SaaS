# Changelog

All notable changes to PrimusSaaS packages will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Identity.Validator Unreleased]

### Added

- `IssuerType.AzureAD` alias for OIDC issuers to improve Azure AD discoverability.

### Changed

- TenantResolver execution is now wrapped in try/catch to return a clear authentication failure instead of a 500 when resolution throws.

---

## [Identity.Validator 1.2.1] - 2025-11-24

### Fixed

- **CRITICAL:** Fixed TenantResolver compilation errors
  - Made `TokenClaims` implement `IEnumerable<KeyValuePair<string, object>>`
  - Added full LINQ support (FirstOrDefault, Where, Select, etc.)
  - TenantResolver feature now fully functional

### Added

- New `TokenClaims` helper methods:
  - `FirstOrDefault()` - Find first matching claim
  - `Where()` - Filter claims
  - `Contains()` - Check if claim exists
  - `Count` - Get number of claims
- Comprehensive `TENANT_RESOLVER_GUIDE.md` documentation
- XML documentation for IntelliSense support

### Changed

- Improved logging with `ILogger` instead of `Console.WriteLine`
- Better error messages for validation failures
- Added `using Microsoft.Extensions.Logging` directive

---

## [Logging 1.1.0] - 2025-11-24

### Fixed

- **CRITICAL:** Fixed dependency version mismatch
  - Downgraded `Microsoft.Extensions.*` from 10.0.0 to 7.0.0
  - Removed obsolete `Microsoft.AspNetCore.App` reference
  - **Result:** Zero build warnings on .NET 7.0 projects

- **HIGH:** Implemented missing `UsePrimusLogging()` middleware
  - Created `ApplicationBuilderExtensions.cs`
  - Middleware enriches logs with HTTP context
  - Automatic user context extraction

- **MEDIUM:** Fixed configuration property naming confusion
  - Added `Format` property as alias for `Pretty`
  - Both configuration styles now work

### Added

- `UsePrimusLogging()` middleware with features:
  - Automatic request ID generation
  - HTTP context enrichment (method, path, status)
  - User context extraction from claims
  - Response header injection (`X-Request-ID`)

- API aliases for flexibility:
  - `AddPrimus()` (original)
  - `AddPrimusLogging()` (alias)

- Comprehensive documentation:
  - `CONFIGURATION_GUIDE.md`
  - `TROUBLESHOOTING.md`
  - `VERIFICATION_GUIDE.md`
  - `QUICK_REFERENCE.md`

### Changed

- Updated README with correct API usage
- Added middleware setup instructions
- Clarified configuration options

---

## [Identity.Validator 1.2.0] - 2025-11-23

### Added

- TenantResolver feature for multi-tenant applications (had compilation issues, fixed in 1.2.1)
- `TokenClaims` class for easier claim access
- `TenantContext` class for tenant information
- Support for tenant-specific roles and metadata

### Changed

- Enhanced multi-tenant support
- Improved claims processing

### Known Issues

- TenantResolver doesn't compile due to missing LINQ support (fixed in 1.2.1)

---

## [Logging 1.0.0] - 2025-11-23

### Added

- Initial release of PrimusSaaS.Logging
- Structured JSON logging with rich context
- Multiple output targets: Console, File, Azure Application Insights
- Log levels: DEBUG, INFO, WARNING, ERROR, CRITICAL
- PII masking for emails, credit cards, SSNs, and custom keys
- File rotation with size-based triggers
- Gzip compression for rotated files
- Async buffering for high-performance logging
- Custom enrichers for dynamic context injection
- Standard `ILogger` and `ILoggerProvider` implementation
- Performance tracking with built-in timers
- Correlation ID generation
- Thread-safe operations

### Known Issues

- Build warnings due to .NET 10.0 dependencies (fixed in 1.1.0)
- `UsePrimusLogging()` middleware not implemented (fixed in 1.1.0)
- Configuration property naming inconsistency (fixed in 1.1.0)

---

## [Identity.Validator 1.1.0] - 2025-11-22

### Added

- Multi-issuer support (Azure AD + Local JWT)
- OIDC discovery support
- JWKS caching
- Comprehensive error messages
- Production deployment guide
- Token generation guide

### Changed

- Improved token validation
- Better error handling
- Enhanced documentation

---

## [Identity.Validator 1.0.0] - 2025-11-21

### Added

- Initial release
- JWT token validation
- Azure AD OIDC support
- `GetPrimusUser()` extension method
- Basic multi-issuer support

---

## Upgrade Guides

### From Identity.Validator 1.2.0 to 1.2.1

**No code changes required!** Simply update:

```bash
dotnet add package PrimusSaaS.Identity.Validator --version 1.2.1
```

If you attempted to use TenantResolver in 1.2.0, you can now use it:

```csharp
options.TenantResolver = claims =>
{
    var tenantId = claims.Get("tid");
    var roles = claims.Where(c => c.Key.StartsWith("role_")).ToList();
    return new TenantContext { TenantId = tenantId ?? "default" };
};
```

### From Logging 1.0.0 to 1.1.0

**No code changes required!** Simply update:

```bash
dotnet add package PrimusSaaS.Logging --version 1.1.0
```

**Optional improvements:**

```csharp
// Add middleware (now works!)
app.UsePrimusLogging();

// Use either API
builder.Logging.AddPrimus(options => { ... });
// OR
builder.Logging.AddPrimusLogging(options => { ... });
```

---

## Support

For issues, questions, or feedback:
- GitHub Issues: https://github.com/primus-saas
- Documentation: https://akkikhan.github.io
- Email: support@primus-saas.com

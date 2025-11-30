# Changelog

All notable changes to @primus-saas/logging will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.2.4] - 2025-11-26

### Changed
- Version bump to align with NuGet Logging 1.2.4; no code changes from 1.2.1.
- Revalidated middleware/enricher/targets through test suite.

---

## [1.2.1] - 2025-11-25

### Added
- Application Insights target (`type: 'application-insights'`) with severity mapping.
- File target rotation (`maxFileSize`, `maxRetainedFiles`, `compressRotatedFiles`) to mirror NuGet behavior.
- PII masking pipeline with strategies (`redact` | `hash` | `partial`) and smart defaults for emails/credit cards/SSN/custom keys.
- Async buffering (`bufferSize`, `flushIntervalMs`, `flushOnExit`) for high-throughput services.
- Express middleware now calls `logger.setRequest` to enrich logs with request/user/tenant context automatically.
- New docs: Configuration Guide, Troubleshooting, Verification Guide, Quick Reference.

### Changed
- Default package version bumped to 1.2.1 to align with NuGet Logging 1.2.1.
- File target now validates path and handles stream errors more defensively.

### Fixed
- Tenant context from Identity Validator now flows through middleware enrichers (`primusTenantContext` supported).
- Console/file logs now apply PII masking before reaching any target.

---

## [1.1.0] - 2025-11-24

### 🎉 Major Improvements

This release adds Express middleware for automatic HTTP context enrichment, matching the .NET package functionality.

### Added

#### Express Middleware
- **`primusLoggingMiddleware(logger)`** - New Express middleware for automatic logging
  - Automatic request ID generation
  - HTTP context enrichment (method, path, query, IP)
  - User context extraction from `req.user`
  - Request/response logging
  - Attached enriched logger to `req.logger`

**Example:**
```typescript
import express from 'express';
import { createLogger, primusLoggingMiddleware } from '@primus-saas/logging';

const app = express();
const logger = createLogger({ 
  applicationId: 'my-app', 
  environment: 'production' 
});

// Add middleware
app.use(primusLoggingMiddleware(logger));

app.get('/api/users', (req, res) => {
  req.logger.info('Fetching users'); // Automatically enriched with HTTP context
  res.json({ users: [] });
});
```

#### TypeScript Support
- Added `@types/express` for proper type definitions
- Express Request interface extended with `logger` property
- Full TypeScript IntelliSense support

#### Package Configuration
- Moved express to peerDependencies (optional)
- Added "middleware" keyword
- Updated documentation files list

### Changed

- Express moved from dependencies to peerDependencies
- Package now supports both Express 4.x and 5.x
- Improved package.json structure

### Documentation

- Added middleware usage examples
- Updated README with middleware documentation
- Improved TypeScript examples

---

## [1.0.0] - 2025-11-23

### Added

- Initial release of @primus-saas/logging
- Structured JSON logging with rich context
- Multiple output targets: Console, File
- Log levels: DEBUG, INFO, WARNING, ERROR, CRITICAL
- PII masking for emails, credit cards, SSNs, and custom keys
- File rotation with size-based triggers
- Async buffering for high-performance logging
- Custom enrichers for dynamic context injection
- Performance tracking with built-in timers
- Correlation ID generation
- Thread-safe operations
- Automatic directory creation for file targets

### Features

- **Console Target**: JSON and formatted output
- **File Target**: Configurable rotation and async writes
- **HTTP Context Enrichment**: Request ID, user, and tenant context (via enrichers)
- **PII Protection**: Regex-based masking with extensible patterns
- **Performance**: Non-blocking writes with configurable buffer sizes

### Dependencies

- No runtime dependencies (lightweight!)
- Express as optional peer dependency

---

## Migration Guides

### From 1.0.0 to 1.1.0

**No breaking changes!** Simply update:

```bash
npm install @primus-saas/logging@1.1.0
```

**Optional improvements:**

1. **Add Express middleware:**
   ```typescript
   import { primusLoggingMiddleware } from '@primus-saas/logging';
   app.use(primusLoggingMiddleware(logger));
   ```

2. **Use enriched logger in routes:**
   ```typescript
   app.get('/api/users', (req, res) => {
     req.logger.info('Request received'); // Automatic context!
     res.json({ users: [] });
   });
   ```

---

## Upgrade Instructions

### Install Latest Version

```bash
npm install @primus-saas/logging@latest
```

### Verify Installation

```bash
npm list @primus-saas/logging
```

Expected output:
```
@primus-saas/logging@1.1.0
```

---

## Support

For issues, questions, or feedback:
- GitHub Issues: https://github.com/primus-saas/logging/issues
- Documentation: https://docs.primus-saas.com/logging
- Email: support@primus-saas.com

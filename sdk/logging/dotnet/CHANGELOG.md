# Changelog

All notable changes to PrimusSaaS.Logging will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-24

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
- ASP.NET Core middleware for automatic HTTP context enrichment
- Performance tracking with built-in timers
- Correlation ID generation
- Thread-safe operations
- Automatic directory creation for file targets

### Features
- **Console Target**: Pretty-printing with colors for development
- **File Target**: Configurable rotation, compression, and async writes
- **Application Insights Target**: Direct integration with Azure Monitor
- **HTTP Context Enrichment**: Automatic request ID, user, and tenant context
- **PII Protection**: Regex-based masking with extensible patterns
- **Performance**: Non-blocking writes with configurable buffer sizes

### Dependencies
- Microsoft.Extensions.Logging >= 7.0.0
- Microsoft.Extensions.Logging.Abstractions >= 7.0.0
- Microsoft.AspNetCore.Http.Abstractions >= 2.2.0
- Microsoft.ApplicationInsights.AspNetCore >= 2.21.0

### Documentation
- Comprehensive README with quick start guides
- Examples for basic usage, ASP.NET Core integration, and ILogger usage
- Best practices and configuration reference

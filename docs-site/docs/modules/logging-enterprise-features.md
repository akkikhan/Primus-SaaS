# Enterprise Features

The Primus Logging SDK includes several features designed for enterprise-scale applications.

## PII Masking

Automatically redact sensitive information from log messages and structured data.

### Default Patterns
- **Emails**: Replaced with `[EMAIL]`
- **Credit Cards**: Replaced with `[CREDIT_CARD]`
- **SSN**: Replaced with `[SSN]` (if configured)

### Custom Patterns
You can define custom Regex patterns to mask other sensitive data (e.g., API keys, internal IDs).

```csharp
// .NET
options.Pii.CustomPatterns.Add(@"Bearer\s+[a-zA-Z0-9\-\._~\+\/]+", "Bearer [REDACTED]");
```

```javascript
// Node.js
new Logger({
  pii: {
    customPatterns: {
      'Bearer\\s+[a-zA-Z0-9\\-\\._~\\+\\/]+': 'Bearer [REDACTED]'
    }
  }
});
```

## File Rotation

Manage disk usage automatically by rotating log files.

- **MaxFileSize**: Triggers rotation when file exceeds size (e.g., 10MB).
- **MaxFiles**: Keeps only the last N rotated files.
- **Compression**: Automatically gzips rotated files to save space.

## Async Buffering

High-volume logging can impact application performance. The SDK supports asynchronous logging with buffering.

- Logs are written to an in-memory buffer first.
- A background thread flushes the buffer to targets (file, network).
- Ensures your application request thread is never blocked by slow I/O.

## Context Enrichment

Automatically adds context to every log entry:
- **Correlation ID**: Trace requests across services.
- **User ID**: If authenticated via Identity Validator.
- **Request Path**: HTTP method and URL.
- **Environment**: Production/Staging/Dev.

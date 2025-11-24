# Logging SDK - Node.js Quick Start

Enterprise-grade structured logging with PII masking and file rotation.

## Installation

```bash
npm install @primus-saas/logging
```

## Express Integration

```javascript
const express = require('express');
const { Logger } = require('@primus-saas/logging');

const app = express();

const logger = new Logger({
  applicationId: 'MY-APP',
  environment: 'production',
  minLevel: 'info',
  targets: [
    { type: 'console', pretty: true },
    { 
      type: 'file', 
      path: 'logs/app.log',
      maxFileSize: 10485760, // 10MB
      rotate: true
    }
  ],
  pii: {
    maskEmails: true,
    maskCreditCards: true
  }
});

// Middleware for request logging
app.use((req, res, next) => {
  logger.setHttpContext(req);
  logger.info(`${req.method} ${req.path}`);
  next();
});

// Use in routes
app.get('/api/data', (req, res) => {
  logger.info('Fetching data', { userId: req.user?.id });
  res.json({ data: [] });
});

app.listen(3000);
```

## Enterprise Features

- **PII Masking**: Automatic redaction of sensitive data
- **File Rotation**: Automatic log file management
- **Context Enrichment**: HTTP request context automatically added
- **Performance Tracking**: Built-in timers
- **Correlation IDs**: For distributed tracing

## Usage Examples

```javascript
// Basic logging
logger.info('User logged in');
logger.error('Failed to process', { errorCode: 'ERR_001' });

// Performance tracking
const timer = logger.startTimer();
await processData();
timer.done('Data processed');

// Correlation IDs
const correlationId = logger.generateCorrelationId();
logger.info('Step 1', { correlationId });
logger.info('Step 2', { correlationId });
```

## Next Steps

- [Configuration Guide](./logging-configuration)
- [Enterprise Features](./logging-enterprise-features)
- [Targets & Outputs](./logging-targets)

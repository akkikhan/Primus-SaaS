# @primus-saas/logging

Enterprise-ready structured logging with PII masking and context enrichment.

> Full client integration guide (Node + .NET + Identity + Logging): see `docs-site/docs/modules/client-integration-guide.md`.

## Installation

```bash
npm install @primus-saas/logging
```

## Quick Start

```typescript
const { createLogger, LogLevel, primusLoggingMiddleware } = require('@primus-saas/logging');
const express = require('express');

const app = express();

// Create logger with multi-target outputs and PII masking
const logger = createLogger({
  applicationId: 'PSP-CLI-711224',
  environment: 'production',
  minLevel: LogLevel.INFO,
  targets: [
    { type: 'console', pretty: true },
    { type: 'file', path: 'logs/app.log', maxFileSize: 5 * 1024 * 1024, maxRetainedFiles: 5 },
    { type: 'application-insights', connectionString: process.env.APPINSIGHTS_CONNECTION_STRING }
  ],
  masking: {
    enabled: true,
    maskEmails: true,
    maskCreditCards: true,
    customSensitiveKeys: ['password', 'apiKey']
  },
  buffering: {
    enabled: true,
    bufferSize: 200,
    flushIntervalMs: 1000
  }
});

// Add Express middleware for request/tenant/user enrichment
app.use(primusLoggingMiddleware(logger));

app.get('/api/users', (req, res) => {
  req.logger.info('Fetching users'); // Enriched with requestId, path, user/tenant (if present)
  res.json({ users: [] });
});

app.listen(3000);
```

## Features

- ✅ **Structured JSON logging** with app + environment context
- ✅ **Request/User/Tenant enrichment** via middleware and enrichers
- ✅ **PII masking** for emails, credit cards, SSNs, and custom keys
- ✅ **File target with rotation & optional gzip compression**
- ✅ **Azure Application Insights target**
- ✅ **Async buffering** for high-throughput services
- ✅ **Performance timers & correlation IDs**
- ✅ **Express middleware** with request ID generation and req.logger helper
- ✅ **Full TypeScript support**

## Express Middleware

```typescript
import express from 'express';
import { createLogger, primusLoggingMiddleware } from '@primus-saas/logging';

const app = express();
const logger = createLogger({
  applicationId: 'MY-APP',
  environment: 'production'
});

app.use(primusLoggingMiddleware(logger));

app.get('/api/orders', (req, res) => {
  req.logger.info('Order requested', { orderId: 'ORD-123' });
  res.json({ ok: true });
});
```

Adds:
- Request ID (existing or generated) on every log
- Method, path, query, IP
- User/tenant context (from `req.user`, `req.primusUser`, `req.primusTenantContext`)
- Response status/duration log on finish

## PII Masking

```typescript
const logger = createLogger({
  applicationId: 'MY-APP',
  environment: 'production',
  masking: {
    enabled: true,
    maskEmails: true,
    maskCreditCards: true,
    maskSSN: true,
    customSensitiveKeys: ['password', 'apiKey'],
    strategy: 'redact' // or 'hash' | 'partial'
  }
});

logger.info('Signup', { email: 'user@example.com', password: 'secret' });
// => context.email / password are masked before hitting any target
```

## File Target with Rotation

```typescript
const logger = createLogger({
  applicationId: 'MY-APP',
  environment: 'production',
  targets: [{
    type: 'file',
    path: 'logs/app.log',
    maxFileSize: 10 * 1024 * 1024, // 10MB
    maxRetainedFiles: 5,
    compressRotatedFiles: true
  }]
});
```

## Azure Application Insights Target

```typescript
const logger = createLogger({
  applicationId: 'MY-APP',
  environment: 'production',
  targets: [{
    type: 'application-insights',
    connectionString: process.env.APPINSIGHTS_CONNECTION_STRING,
    roleName: 'api-service'
  }]
});
```

## Async Buffering

```typescript
const logger = createLogger({
  applicationId: 'MY-APP',
  environment: 'production',
  buffering: {
    enabled: true,
    bufferSize: 250,       // flush after 250 entries
    flushIntervalMs: 1000, // or after 1 second
    flushOnExit: true
  }
});
```

## Usage

### Basic Logging

```typescript
logger.debug('Detailed diagnostic information');
logger.info('General informational message');
logger.warn('Warning message');
logger.error('Error message');
logger.critical('Critical error!');
```

### Logging with Context

```typescript
logger.info('Order created', {
  orderId: 'ORD-123',
  userId: '456',
  amount: 99.99
});

// Output:
{
  "timestamp": "2025-11-24T03:15:49.123Z",
  "level": "INFO",
  "message": "Order created",
  "context": {
    "applicationId": "PSP-CLI-711224",
    "environment": "production",
    "orderId": "ORD-123",
    "userId": "456",
    "amount": 99.99
  }
}
```

### Performance Tracking

```typescript
const timer = logger.startTimer();

await processOrder(orderId);

timer.done('Order processed', { orderId });

// Output includes "duration" in milliseconds
```

### Correlation IDs (Microservices)

```typescript
// Service A
const correlationId = logger.generateCorrelationId();
logger.info('Checkout started', { correlationId });

// Pass to Service B
await axios.post('http://service-b/api', data, {
  headers: { 'X-Correlation-ID': correlationId }
});

// Service B
const correlationId = req.headers['x-correlation-id'];
logger.info('Processing request', { correlationId });

// Search logs by correlationId to see entire flow
```

### Log Level Filtering

```typescript
// Development: Log everything
const devLogger = createLogger({
  applicationId: 'APP-123',
  environment: 'development',
  minLevel: LogLevel.DEBUG
});

// Production: Only INFO and above
const prodLogger = createLogger({
  applicationId: 'APP-123',
  environment: 'production',
  minLevel: LogLevel.INFO
});

prodLogger.debug('This will be filtered out');
prodLogger.info('This will be logged');
```

## Configuration

```typescript
interface LoggerOptions {
  /** Application ID from Primus Portal (required) */
  applicationId: string;
  
  /** Environment (required) */
  environment: 'development' | 'testing' | 'production';
  
  /** Minimum log level (default: INFO) */
  minLevel?: LogLevel;
}
```

## Output Format

All logs are output as structured JSON:

```json
{
  "timestamp": "2025-11-24T03:15:49.123Z",
  "level": "INFO",
  "message": "User logged in",
  "context": {
    "applicationId": "PSP-CLI-711224",
    "environment": "production",
    "userId": "12345"
  }
}
```

## Examples

See the `examples/` directory for complete examples:
- `basic-usage.js` - Basic logging features
- More examples coming soon!

## License

MIT

## Support

For issues and questions, please visit: https://portal.primus-saas.com

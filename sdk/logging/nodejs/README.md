# @primus-saas/logging

Enterprise-ready structured logging with PII masking and context enrichment.

> Full client integration guide (Node + .NET + Identity + Logging): see `docs-site/docs/modules/client-integration-guide.md`.

## Installation

```bash
npm install @primus-saas/logging
```

## Quick Start

```typescript
const { createLogger, LogLevel } = require('@primus-saas/logging');

// Create logger
const logger = createLogger({
  applicationId: 'PSP-CLI-711224', // From Primus Portal
  environment: 'production',
  minLevel: LogLevel.INFO
});

// Log messages
logger.info('User logged in', { userId: '12345' });
logger.error('Operation failed', { error: 'Database timeout' });
```

## Features

- ✅ **Structured Logging** - Consistent JSON format
- ✅ **Auto Context Enrichment** - Timestamp, applicationId, environment
- ✅ **Log Levels** - DEBUG, INFO, WARNING, ERROR, CRITICAL
- ✅ **Performance Tracking** - Built-in timers
- ✅ **Correlation IDs** - For distributed tracing
- ✅ **Log Level Filtering** - Control verbosity

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

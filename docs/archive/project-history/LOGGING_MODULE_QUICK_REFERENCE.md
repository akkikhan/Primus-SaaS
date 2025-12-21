# 📝 Primus Logging Module - Quick Reference (SIMPLIFIED)

**Version**: 1.0.0  
**Last Updated**: November 24, 2025  
**Philosophy**: Keep It Simple

---

## ⚡ Quick Start (< 5 Minutes)

### Installation

```bash
# Node.js
npm install @primus-saas/logging@1.0.0

# .NET
dotnet add package PrimusSaaS.Logging --version 1.0.0
```

### Basic Setup

**Node.js**:
```javascript
const { createLogger } = require('@primus-saas/logging');

const logger = createLogger({
  applicationId: 'PSP-CLI-XXXXXX', // From Portal
  environment: 'production',
  minLevel: 'INFO',
  targets: [
    { type: 'console' },
    { type: 'file', path: './logs/app.log' }
  ]
});

logger.info('Application started');
```

**.NET**:
```csharp
using PrimusSaaS.Logging;

var logger = PrimusLogger.CreateLogger(new LoggerOptions {
    ApplicationId = "PSP-CLI-XXXXXX",
    Environment = "production",
    MinLevel = LogLevel.Info,
    Targets = new[] {
        new ConsoleTarget(),
        new FileTarget { Path = "./logs/app.log" }
    }
});

logger.Info("Application started");
```

---

## 📊 Log Levels

| Level | Method | Use Case |
|-------|--------|----------|
| DEBUG | `logger.debug()` | Detailed diagnostics |
| INFO | `logger.info()` | General messages |
| WARNING | `logger.warn()` | Potential issues |
| ERROR | `logger.error()` | Errors |
| CRITICAL | `logger.critical()` | Severe errors |

---

## 💡 Common Patterns

### 1. Basic Logging
```javascript
logger.info('User logged in');
logger.error('Failed to connect to database');
```

### 2. Logging with Context
```javascript
logger.info('Order created', {
  orderId: '12345',
  userId: 'user-abc',
  amount: 99.99
});
```

### 3. Error Logging
```javascript
try {
  await riskyOperation();
} catch (error) {
  logger.error('Operation failed', {
    error: error.message,
    stack: error.stack
  });
}
```

### 4. Performance Tracking
```javascript
const timer = logger.startTimer();
await processOrder(orderId);
timer.done('Order processed'); // Logs execution time
```

### 5. Correlation IDs (Microservices)
```javascript
// Service A
const correlationId = logger.generateCorrelationId();
logger.info('Request started', { correlationId });

// Pass to Service B
await axios.post('http://service-b/api', data, {
  headers: { 'X-Correlation-ID': correlationId }
});

// Service B
const correlationId = req.headers['x-correlation-id'];
logger.info('Processing request', { correlationId });
```

---

## 🎯 Output Targets

### Console (Development)
```javascript
logger.configure({
  targets: [{ type: 'console' }]
});
```

### File (Production)
```javascript
logger.configure({
  targets: [
    { type: 'file', path: '/var/log/app.log' }
  ]
});
```

### Multiple Targets
```javascript
logger.configure({
  targets: [
    { type: 'console' },
    { type: 'file', path: './logs/app.log' }
  ]
});
```

**Note**: For cloud logging (CloudWatch, Azure, etc.), write to files and use agents (CloudWatch agent, Fluentd, etc.) to ship logs.

---

## 🔐 PII Masking

### Enable Masking
```javascript
logger.configure({
  masking: {
    enabled: true,
    fields: ['password', 'ssn', 'creditCard', 'apiKey', 'secret']
  }
});

logger.info('User created', {
  username: 'john.doe',
  password: 'SuperSecret123', // → ***REDACTED***
  email: 'john@example.com'   // → Not masked
});
```

---

## 🔗 Integration with Identity Validator

```javascript
const { primusIdentityValidator } = require('primus-identity-validator');
const logger = require('./logger');

const primusAuth = primusIdentityValidator({ ... });

app.get('/api/orders', primusAuth, (req, res) => {
  // Logger automatically includes:
  // - req.primusUser (userId, email, roles)
  // - req.primusTenantContext (tenantId, tenantName)
  
  logger.info('Fetching orders'); // Auto-enriched!
  
  res.json({ orders: [] });
});
```

---

## 📈 Output Format

### Structured JSON
```json
{
  "timestamp": "2025-11-24T02:50:53.123Z",
  "level": "INFO",
  "message": "User logged in",
  "context": {
    "userId": "12345",
    "tenantId": "acme-corp",
    "requestId": "req-abc-123",
    "correlationId": "corr-xyz-789",
    "applicationId": "PSP-CLI-711224",
    "environment": "production"
  }
}
```

---

## 🚀 Production Configuration Example

```javascript
const { createLogger } = require('@primus-saas/logging');

const logger = createLogger({
  // Required
  applicationId: 'PSP-CLI-711224',
  environment: 'production',
  
  // Log level
  minLevel: 'INFO', // Don't log DEBUG in production
  
  // Output targets
  targets: [
    { 
      type: 'file', 
      path: '/var/log/acme/app.log'
    }
  ],
  
  // PII masking
  masking: {
    enabled: true,
    fields: ['password', 'ssn', 'creditCard', 'apiKey', 'secret']
  }
});

module.exports = logger;
```

**Then use CloudWatch agent or Fluentd to ship logs to cloud.**

---

## 🐛 Troubleshooting

### Logs Not Appearing?

1. **Check log level**:
   ```javascript
   logger.configure({ minLevel: 'DEBUG' });
   ```

2. **Check targets**:
   ```javascript
   logger.configure({
     targets: [{ type: 'console' }] // Simplest target
   });
   ```

### Performance Issues?

1. **Reduce log level in production**:
   ```javascript
   logger.configure({ minLevel: 'INFO' }); // Skip DEBUG logs
   ```

---

## ✅ Integration Checklist

- [ ] Install package (`npm install @primus-saas/logging`)
- [ ] Get `applicationId` from Portal
- [ ] Create logger instance with basic config
- [ ] Test logging in development (console target)
- [ ] Configure production target (file)
- [ ] Enable PII masking
- [ ] Test with Identity Validator integration
- [ ] Configure correlation IDs for microservices
- [ ] Deploy and monitor

---

## 📚 Resources

- **Full Documentation**: See `LOGGING_MODULE_IMPLEMENTATION_PLAN.md`
- **Portal**: https://portal.primus-saas.com
- **NPM Package**: https://www.npmjs.com/package/@primus-saas/logging
- **NuGet Package**: https://www.nuget.org/packages/PrimusSaaS.Logging

---

**Quick Reference Version**: 2.0 (Simplified)  
**Module Version**: 1.0.0  
**Last Updated**: November 24, 2025

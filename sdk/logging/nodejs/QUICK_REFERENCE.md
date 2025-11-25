# Quick Reference - @primus-saas/logging (Node.js)

## Install
```bash
npm install @primus-saas/logging
```

## Create logger
```typescript
const logger = createLogger({
  applicationId: 'MY-APP',
  environment: 'production',
  minLevel: LogLevel.INFO
});
```

## Middleware
```typescript
app.use(primusLoggingMiddleware(logger));
```

## Targets
- Console: `{ type: 'console', pretty: true }`
- File: `{ type: 'file', path: 'logs/app.log', maxFileSize, maxRetainedFiles, compressRotatedFiles }`
- App Insights: `{ type: 'application-insights', connectionString, roleName }`

## Masking
```typescript
masking: { enabled: true, maskEmails: true, customSensitiveKeys: ['password'] }
```

## Buffering
```typescript
buffering: { enabled: true, bufferSize: 200, flushIntervalMs: 1000 }
```

## Helpers
- `logger.startTimer()` → `.done('message', { extra })`
- `logger.generateCorrelationId()` for distributed tracing

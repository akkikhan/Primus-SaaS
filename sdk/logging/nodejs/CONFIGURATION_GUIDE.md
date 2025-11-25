# Configuration Guide - @primus-saas/logging (Node.js)

This guide mirrors the NuGet configuration but for Node.js/Express services.

## Minimal setup

```typescript
import { createLogger } from '@primus-saas/logging';

export const logger = createLogger({
  applicationId: 'MY-APP',
  environment: 'development'
});
```

## Targets

```typescript
targets: [
  { type: 'console', pretty: true },
  { type: 'file', path: 'logs/app.log', maxFileSize: 10 * 1024 * 1024, maxRetainedFiles: 5, compressRotatedFiles: true },
  { type: 'application-insights', connectionString: process.env.APPINSIGHTS_CONNECTION_STRING, roleName: 'api-service' }
]
```

## PII masking

```typescript
masking: {
  enabled: true,
  maskEmails: true,
  maskCreditCards: true,
  maskSSN: true,
  customSensitiveKeys: ['password', 'apiKey'],
  strategy: 'redact' // or 'hash' | 'partial'
}
```

## Async buffering

```typescript
buffering: {
  enabled: true,
  bufferSize: 250,
  flushIntervalMs: 1000,
  flushOnExit: true
}
```

## Middleware

```typescript
import { primusLoggingMiddleware } from '@primus-saas/logging';
app.use(primusLoggingMiddleware(logger));
```

Adds requestId, method, path, query, IP, and user/tenant context (`req.primusUser` / `req.primusTenantContext`).

## Custom enrichers

```typescript
class MachineNameEnricher {
  enrich(context: Record<string, any>) {
    return { ...context, machine: require('os').hostname() };
  }
}

const logger = createLogger({ ..., enrichers: [new MachineNameEnricher()] });
```

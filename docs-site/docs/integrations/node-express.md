---
id: node-express
title: Node.js (Express) Integration
---

# Node.js (Express) Integration Guide

This guide shows you how to integrate Primus SaaS modules with your Express.js application.

## Available Modules

### Identity Validator
Multi-issuer JWT/OIDC token validator for authenticating users.

**[View Full Documentation →](/docs/modules/identity-validator-nodejs)**

### Logging SDK
Enterprise-grade structured logging with PII masking and file rotation.

**[View Full Documentation →](/docs/modules/logging-nodejs)**

---

## Quick Start: Identity Validator

### Installation

```bash
npm install primus-identity-validator express
```

### Basic Setup

```javascript
const express = require('express');
const { PrimusIdentityValidator } = require('primus-identity-validator');

const app = express();

const validator = new PrimusIdentityValidator({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/<TENANT_ID>/v2.0',
      authority: 'https://login.microsoftonline.com/<TENANT_ID>/v2.0',
      audiences: ['api://your-api-id']
    }
  ]
});

// Apply middleware
app.use(validator.middleware());

// Public endpoint
app.get('/api/public', (req, res) => {
  res.json({ message: 'No auth required' });
});

// Protected endpoint
app.get('/api/protected', validator.requireAuth(), (req, res) => {
  res.json({
    message: 'Authenticated',
    user: req.primusUser
  });
});

app.listen(3000);
```

**[Full Identity Validator Documentation →](/docs/modules/identity-validator-nodejs)**

---

## Quick Start: Logging SDK

### Installation

```bash
npm install @primus-saas/logging
```

### Basic Setup

```javascript
const express = require('express');
const { Logger } = require('@primus-saas/logging');

const app = express();

const logger = new Logger({
  applicationId: 'MY-APP',
  environment: 'production',
  targets: [
    { type: 'console', pretty: true },
    { type: 'file', path: 'logs/app.log' }
  ]
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

**[Full Logging SDK Documentation →](/docs/modules/logging-nodejs)**

---

## Using Both Modules Together

```javascript
const express = require('express');
const { PrimusIdentityValidator } = require('primus-identity-validator');
const { Logger } = require('@primus-saas/logging');

const app = express();

// Setup logging
const logger = new Logger({
  applicationId: 'MY-APP',
  environment: 'production',
  targets: [
    { type: 'console', pretty: true },
    { type: 'file', path: 'logs/app.log', async: true }
  ],
  pii: {
    maskEmails: true,
    maskCreditCards: true
  }
});

// Setup authentication
const validator = new PrimusIdentityValidator({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/<TENANT_ID>/v2.0',
      authority: 'https://login.microsoftonline.com/<TENANT_ID>/v2.0',
      audiences: ['api://your-api-id']
    }
  ]
});

// Apply middleware
app.use(validator.middleware());
app.use((req, res, next) => {
  logger.setHttpContext(req);
  next();
});

// Protected route with logging
app.get('/api/data', validator.requireAuth(), (req, res) => {
  logger.info('User accessed data', {
    userId: req.primusUser.userId,
    email: req.primusUser.email
  });
  
  res.json({ data: [] });
});

app.listen(3000, () => {
  logger.info('Server started on port 3000');
});
```

---

## Production Best Practices

### Security
- Use HTTPS in production
- Store secrets in environment variables
- Enable token lifetime validation
- Implement rate limiting

### Logging
- Enable PII masking for sensitive data
- Use async file targets for performance
- Configure file rotation to manage disk space
- Send critical logs to Application Insights

### Monitoring
- Track authentication failures
- Monitor API response times
- Set up alerts for errors
- Review logs regularly

---

## Next Steps

- **Identity Validator**: [Full Documentation](/docs/modules/identity-validator-nodejs)
- **Logging SDK**: [Full Documentation](/docs/modules/logging-nodejs)
- **Module Mapping**: [View Available Modules](/docs/module-mapping)

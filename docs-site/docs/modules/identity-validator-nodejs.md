# Identity Validator - Node.js Quick Start

Multi-issuer JWT/OIDC token validator for Express and NestJS applications.

## Installation

```bash
npm install primus-identity-validator
```

## Express Integration

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

// Protect routes
app.get('/api/secure', validator.requireAuth(), (req, res) => {
  res.json({
    userId: req.primusUser.userId,
    email: req.primusUser.email
  });
});

app.listen(3000);
```

## NestJS Integration

```typescript
import { Module } from '@nestjs/common';
import { PrimusIdentityModule } from 'primus-identity-validator/nestjs';

@Module({
  imports: [
    PrimusIdentityModule.forRoot({
      issuers: [
        {
          name: 'AzureAD',
          type: 'oidc',
          issuer: 'https://login.microsoftonline.com/<TENANT_ID>/v2.0',
          authority: 'https://login.microsoftonline.com/<TENANT_ID>/v2.0',
          audiences: ['api://your-api-id']
        }
      ]
    })
  ]
})
export class AppModule {}
```

## Next Steps

- [Full Configuration Guide](./identity-configuration)
- [Token Generation](./identity-token-generation)
- [Error Reference](./identity-error-reference)
- [Production Deployment](./identity-production-deployment)

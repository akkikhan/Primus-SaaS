# Production Deployment Guide - Node.js SDK

Best practices for deploying **primus-identity-validator** to production environments.

> [!CAUTION]
> **Never commit secrets to source control!** Use environment variables, secret management services, or secure vaults.

## Table of Contents

1. [Secret Management](#secret-management)
2. [Environment Configuration](#environment-configuration)
3. [HTTPS Requirements](#https-requirements)
4. [CORS Configuration](#cors-configuration)
5. [Monitoring and Logging](#monitoring-and-logging)
6. [Performance Optimization](#performance-optimization)
7. [Deployment Checklist](#deployment-checklist)

---

## Secret Management

### Environment Variables (Recommended)

#### 1. Use dotenv for Development

```bash
npm install dotenv
```

**.env** (Development - never commit!)
```env
JWT_SECRET=dev-secret-key-at-least-32-characters-long
JWT_ISSUER=https://localhost:4000
JWT_AUDIENCE=api://dev-app-id

AZURE_TENANT_ID=your-dev-tenant-id
AZURE_CLIENT_ID=your-dev-client-id
```

**Load in your app:**
```javascript
require('dotenv').config();

const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'LocalAuth',
      type: 'jwt',
      issuer: process.env.JWT_ISSUER,
      secret: process.env.JWT_SECRET,
      audiences: [process.env.JWT_AUDIENCE]
    }
  ]
});
```

#### 2. Production Environment Variables

**Azure App Service:**
```bash
az webapp config appsettings set \
  --name your-app \
  --resource-group your-rg \
  --settings \
    JWT_SECRET="production-secret-key" \
    JWT_ISSUER="https://api.yourdomain.com" \
    JWT_AUDIENCE="api://prod-app-id"
```

**AWS Elastic Beanstalk:**
```bash
eb setenv \
  JWT_SECRET="production-secret-key" \
  JWT_ISSUER="https://api.yourdomain.com" \
  JWT_AUDIENCE="api://prod-app-id"
```

**Docker:**
```bash
docker run \
  -e JWT_SECRET="production-secret-key" \
  -e JWT_ISSUER="https://api.yourdomain.com" \
  -e JWT_AUDIENCE="api://prod-app-id" \
  your-image
```

**Kubernetes:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: primus-secrets
type: Opaque
stringData:
  JWT_SECRET: "production-secret-key"
  JWT_ISSUER: "https://api.yourdomain.com"
  JWT_AUDIENCE: "api://prod-app-id"
```

---

### AWS Secrets Manager

```bash
npm install @aws-sdk/client-secrets-manager
```

```javascript
const { SecretsManagerClient, GetSecretValueCommand } = require('@aws-sdk/client-secrets-manager');

async function loadSecrets() {
  const client = new SecretsManagerClient({ region: 'us-east-1' });
  
  const response = await client.send(
    new GetSecretValueCommand({ SecretId: 'primus-identity-secrets' })
  );
  
  const secrets = JSON.parse(response.SecretString);
  
  return {
    jwtSecret: secrets.JWT_SECRET,
    jwtIssuer: secrets.JWT_ISSUER,
    jwtAudience: secrets.JWT_AUDIENCE
  };
}

// Use in app
(async () => {
  const secrets = await loadSecrets();
  
  const primusAuth = primusIdentityMiddleware({
    issuers: [{
      name: 'LocalAuth',
      type: 'jwt',
      issuer: secrets.jwtIssuer,
      secret: secrets.jwtSecret,
      audiences: [secrets.jwtAudience]
    }]
  });
  
  app.use(primusAuth);
})();
```

---

### Azure Key Vault

```bash
npm install @azure/keyvault-secrets @azure/identity
```

```javascript
const { SecretClient } = require('@azure/keyvault-secrets');
const { DefaultAzureCredential } = require('@azure/identity');

async function loadSecretsFromKeyVault() {
  const vaultUrl = `https://${process.env.KEY_VAULT_NAME}.vault.azure.net`;
  const credential = new DefaultAzureCredential();
  const client = new SecretClient(vaultUrl, credential);
  
  const jwtSecret = await client.getSecret('JWT-SECRET');
  const jwtIssuer = await client.getSecret('JWT-ISSUER');
  const jwtAudience = await client.getSecret('JWT-AUDIENCE');
  
  return {
    jwtSecret: jwtSecret.value,
    jwtIssuer: jwtIssuer.value,
    jwtAudience: jwtAudience.value
  };
}
```

---

## Environment Configuration

### Config Module Pattern

**config/default.js**
```javascript
module.exports = {
  server: {
    port: 4000,
    env: 'development'
  },
  auth: {
    clockSkew: 300,
    validateLifetime: true,
    jwksCacheTtl: 24
  }
};
```

**config/production.js**
```javascript
module.exports = {
  server: {
    port: process.env.PORT || 8080,
    env: 'production'
  },
  auth: {
    issuers: [
      {
        name: 'AzureAD',
        type: 'oidc',
        issuer: process.env.AZURE_ISSUER,
        authority: process.env.AZURE_AUTHORITY,
        audiences: [process.env.AZURE_AUDIENCE]
      }
    ],
    clockSkew: 300,
    validateLifetime: true
  }
};
```

**Usage:**
```javascript
const config = require('config');

const primusAuth = primusIdentityMiddleware(config.get('auth'));
```

---

### Validate Configuration on Startup

```javascript
function validateConfig() {
  const required = ['JWT_SECRET', 'JWT_ISSUER', 'JWT_AUDIENCE'];
  const missing = required.filter(key => !process.env[key]);
  
  if (missing.length > 0) {
    console.error(`❌ Missing required environment variables: ${missing.join(', ')}`);
    process.exit(1);
  }
  
  if (process.env.JWT_SECRET.length < 32) {
    console.error('❌ JWT_SECRET must be at least 32 characters long');
    process.exit(1);
  }
  
  console.log('✅ Configuration validated successfully');
}

validateConfig();
```

---

## HTTPS Requirements

### Force HTTPS in Production

```javascript
const express = require('express');
const app = express();

// Redirect HTTP to HTTPS in production
if (process.env.NODE_ENV === 'production') {
  app.use((req, res, next) => {
    if (req.header('x-forwarded-proto') !== 'https') {
      res.redirect(`https://${req.header('host')}${req.url}`);
    } else {
      next();
    }
  });
}
```

### HSTS (HTTP Strict Transport Security)

```javascript
const helmet = require('helmet');

app.use(helmet.hsts({
  maxAge: 31536000,  // 1 year
  includeSubDomains: true,
  preload: true
}));
```

### SSL Certificate Setup

#### Using Let's Encrypt

```bash
npm install greenlock-express
```

```javascript
const greenlockExpress = require('greenlock-express');

greenlockExpress.init({
  packageRoot: __dirname,
  configDir: './greenlock.d',
  maintainerEmail: 'admin@yourdomain.com',
  cluster: false
}).serve(app);
```

#### Manual Certificate

```javascript
const https = require('https');
const fs = require('fs');

const options = {
  key: fs.readFileSync('path/to/private-key.pem'),
  cert: fs.readFileSync('path/to/certificate.pem')
};

https.createServer(options, app).listen(443);
```

---

## CORS Configuration

### Production CORS Setup

```javascript
const cors = require('cors');

const corsOptions = {
  origin: function (origin, callback) {
    const allowedOrigins = [
      'https://yourdomain.com',
      'https://app.yourdomain.com'
    ];
    
    if (!origin || allowedOrigins.indexOf(origin) !== -1) {
      callback(null, true);
    } else {
      callback(new Error('Not allowed by CORS'));
    }
  },
  credentials: true,
  methods: ['GET', 'POST', 'PUT', 'DELETE'],
  allowedHeaders: ['Content-Type', 'Authorization']
};

app.use(cors(corsOptions));
```

### Environment-Specific CORS

```javascript
const corsOptions = process.env.NODE_ENV === 'production'
  ? {
      origin: process.env.ALLOWED_ORIGINS.split(','),
      credentials: true
    }
  : {
      origin: '*'  // Allow all in development
    };

app.use(cors(corsOptions));
```

---

## Monitoring and Logging

### Winston Logger

```bash
npm install winston
```

```javascript
const winston = require('winston');

const logger = winston.createLogger({
  level: process.env.LOG_LEVEL || 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.json()
  ),
  transports: [
    new winston.transports.File({ filename: 'error.log', level: 'error' }),
    new winston.transports.File({ filename: 'combined.log' })
  ]
});

if (process.env.NODE_ENV !== 'production') {
  logger.add(new winston.transports.Console({
    format: winston.format.simple()
  }));
}

// Log authentication events
app.use((req, res, next) => {
  if (req.primusUser) {
    logger.info('Authenticated request', {
      userId: req.primusUser.userId,
      path: req.path,
      method: req.method
    });
  }
  next();
});
```

### Application Insights (Azure)

```bash
npm install applicationinsights
```

```javascript
const appInsights = require('applicationinsights');

if (process.env.APPLICATIONINSIGHTS_CONNECTION_STRING) {
  appInsights.setup(process.env.APPLICATIONINSIGHTS_CONNECTION_STRING)
    .setAutoDependencyCorrelation(true)
    .setAutoCollectRequests(true)
    .setAutoCollectPerformance(true)
    .setAutoCollectExceptions(true)
    .setAutoCollectDependencies(true)
    .start();
}
```

### Health Check Endpoint

```javascript
app.get('/health', (req, res) => {
  const health = {
    uptime: process.uptime(),
    timestamp: Date.now(),
    status: 'OK',
    environment: process.env.NODE_ENV,
    configValid: !!process.env.JWT_SECRET
  };
  
  res.status(200).json(health);
});
```

### Error Tracking

```javascript
// Global error handler
app.use((err, req, res, next) => {
  logger.error('Unhandled error', {
    error: err.message,
    stack: err.stack,
    path: req.path,
    method: req.method
  });
  
  res.status(500).json({
    error: process.env.NODE_ENV === 'production' 
      ? 'Internal server error' 
      : err.message
  });
});
```

---

## Performance Optimization

### Compression

```bash
npm install compression
```

```javascript
const compression = require('compression');

app.use(compression());
```

### Rate Limiting

```bash
npm install express-rate-limit
```

```javascript
const rateLimit = require('express-rate-limit');

const limiter = rateLimit({
  windowMs: 15 * 60 * 1000,  // 15 minutes
  max: 100,  // Limit each IP to 100 requests per windowMs
  message: 'Too many requests from this IP'
});

app.use('/api/', limiter);
```

### Caching

```javascript
const primusAuth = primusIdentityMiddleware({
  issuers: [...],
  jwksCacheTtl: 24  // Cache JWKS for 24 hours
});
```

### Clustering

```javascript
const cluster = require('cluster');
const os = require('os');

if (cluster.isMaster && process.env.NODE_ENV === 'production') {
  const numCPUs = os.cpus().length;
  
  for (let i = 0; i < numCPUs; i++) {
    cluster.fork();
  }
  
  cluster.on('exit', (worker, code, signal) => {
    console.log(`Worker ${worker.process.pid} died`);
    cluster.fork();  // Restart worker
  });
} else {
  // Start Express app
  app.listen(process.env.PORT || 4000);
}
```

---

## Deployment Checklist

### Pre-Deployment

- [ ] **Secrets Management**
  - [ ] All secrets in environment variables or vault
  - [ ] No secrets in code or .env files committed
  - [ ] `.env` in `.gitignore`
  
- [ ] **HTTPS Configuration**
  - [ ] SSL certificate configured
  - [ ] HTTPS redirection enabled in production
  - [ ] HSTS enabled

- [ ] **Configuration Validation**
  - [ ] Environment variables validated on startup
  - [ ] Issuer URLs point to production identity providers
  - [ ] Audience values match production app registrations
  - [ ] Clock skew set appropriately (300 seconds)

- [ ] **CORS Configuration**
  - [ ] Allowed origins limited to production domains
  - [ ] No wildcard `*` origins in production
  - [ ] Credentials enabled if needed

- [ ] **Logging**
  - [ ] Logger configured (Winston, Application Insights, etc.)
  - [ ] Log levels appropriate for production (`info` or `warn`)
  - [ ] Health check endpoint exposed
  - [ ] Error tracking enabled

- [ ] **Security**
  - [ ] Helmet.js installed and configured
  - [ ] Rate limiting enabled
  - [ ] Input validation on all endpoints
  - [ ] SQL injection protection (if using database)

- [ ] **Testing**
  - [ ] Integration tests pass with production-like config
  - [ ] Token validation tested with production issuers
  - [ ] Load testing completed
  - [ ] CORS tested from production frontend

### Post-Deployment

- [ ] **Monitoring**
  - [ ] Health check endpoint returning 200 OK
  - [ ] Logs showing successful authentications
  - [ ] No unexpected 401/403 errors
  - [ ] Application Insights/monitoring showing telemetry

- [ ] **Security Verification**
  - [ ] HTTPS enforced (no HTTP access)
  - [ ] Secrets not exposed in logs or error messages
  - [ ] CORS working correctly (no CORS errors)
  - [ ] Rate limiting working

- [ ] **Performance**
  - [ ] JWKS caching working (check logs)
  - [ ] Response times acceptable
  - [ ] No memory leaks
  - [ ] CPU usage normal

---

## Troubleshooting Production Issues

### Enable Debug Logging Temporarily

```javascript
// Set via environment variable
process.env.LOG_LEVEL = 'debug';

// Or in code
logger.level = 'debug';
```

> [!WARNING]
> Revert to `info` or `warn` after troubleshooting to avoid excessive log volume.

### Check Environment Variables

```javascript
app.get('/debug/env', (req, res) => {
  res.json({
    nodeEnv: process.env.NODE_ENV,
    hasJwtSecret: !!process.env.JWT_SECRET,
    jwtIssuer: process.env.JWT_ISSUER,
    jwtAudience: process.env.JWT_AUDIENCE
    // Don't expose actual secret values!
  });
});
```

### Test Token Validation

```javascript
const jwt = require('jsonwebtoken');

app.post('/debug/validate-token', (req, res) => {
  const { token } = req.body;
  
  try {
    const decoded = jwt.verify(token, process.env.JWT_SECRET, {
      issuer: process.env.JWT_ISSUER,
      audience: process.env.JWT_AUDIENCE
    });
    res.json({ valid: true, decoded });
  } catch (error) {
    res.json({ valid: false, error: error.message });
  }
});
```

---

## Additional Resources

- [TOKEN_GENERATION_GUIDE.md](./TOKEN_GENERATION_GUIDE.md) - Token generation examples
- [ERROR_REFERENCE.md](./ERROR_REFERENCE.md) - Troubleshooting validation errors
- [Express Production Best Practices](https://expressjs.com/en/advanced/best-practice-performance.html)
- [Node.js Security Checklist](https://cheatsheetseries.owasp.org/cheatsheets/Nodejs_Security_Cheat_Sheet.html)

---

**Need Help?**
- GitHub Issues: https://github.com/akkikhan/Primus-SaaS/issues
- Documentation: https://portal.primus-saas.com/docs

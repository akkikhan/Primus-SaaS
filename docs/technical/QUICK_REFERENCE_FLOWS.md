# 🚀 Primus SaaS - Quick Reference Guide

**One-Page Overview of Client App Flows**

---

## 📦 Module 1: Identity Validator

### Setup (5 minutes)
```bash
# .NET
dotnet add package PrimusSaaS.Identity.Validator

# Node.js
npm install primus-identity-validator
```

### Configure
```javascript
// Node.js
const validator = new IdentityValidator({
  mode: 'AzureAd',           // or 'Local' or 'Hybrid'
  tenantId: 'your-tenant-id',
  audience: 'api://your-app-id'
});
app.use(validator.middleware());
```

```csharp
// .NET
builder.Services.AddPrimusIdentityValidator(options => {
    options.Mode = AuthMode.AzureAd;
    options.TenantId = "your-tenant-id";
    options.Audience = "api://your-app-id";
});
app.UsePrimusIdentityValidator();
```

### Benefits
- ✅ <5ms validation (cached)
- ✅ Offline capable
- ✅ Multi-tenant ready
- ✅ Zero vendor lock-in

---

## 📝 Module 2: Logging

### Setup (5 minutes)
```bash
# .NET
dotnet add package PrimusSaaS.Logging

# Node.js
npm install @primus-saas/logging
```

### Configure
```javascript
// Node.js
const logger = createLogger({
  applicationId: 'PSP-CLI-711224',
  environment: 'production',
  minLevel: 'INFO',
  targets: [
    { type: 'console' },
    { type: 'file', path: './logs/app.log' }
  ]
});

logger.info('User logged in', { userId: '123' });
```

```csharp
// .NET
builder.Services.AddPrimusLogging(options => {
    options.ApplicationId = "PSP-CLI-711224";
    options.Environment = "production";
    options.MinLevel = LogLevel.Information;
});

logger.LogInformation("User logged in", new { UserId = "123" });
```

### Benefits
- ✅ <1ms overhead
- ✅ Auto PII masking
- ✅ Structured JSON
- ✅ Correlation IDs

---

## 🔄 Integration Flows

### Identity Validator Flow
```
Email → Install → Configure → Test → Deploy
  1min    1min      2min      1min   varies
```

### Logging Flow
```
Install → Configure → Replace Logs → Verify → Deploy
  1min      2min        2min        1min    varies
```

### Combined Flow
```
Auth validates → User info extracted → Logger auto-enriches → Complete audit trail
    <5ms              instant              <1ms                  automatic
```

---

## 🎯 Key Metrics

| Metric | Value |
|--------|-------|
| Setup Time | 5 minutes per module |
| Auth Latency (cached) | <5ms |
| Auth Latency (uncached) | <50ms |
| Logging Overhead | <1ms |
| Cache Hit Rate | >99% |
| Time Savings | 95% vs custom build |
| Cost Savings | $30K+ per app |

---

## 🆘 Common Issues

### Issue: Invalid Signature
**Fix:** Check tenant ID and JWKS endpoint

### Issue: Token Expired
**Fix:** Verify system time and token refresh flow

### Issue: Wrong Audience
**Fix:** Match audience in config with Azure AD app ID

### Issue: Logs Missing Context
**Fix:** Ensure Identity Validator runs before logging

---

## 📞 Quick Links

- **Documentation**: `CLIENT_APP_FLOW_DIAGRAMS.md`
- **Summary**: `FLOW_DIAGRAMS_SUMMARY.md`
- **Error Guide**: `ERROR_REFERENCE.md`
- **Support**: support@primussaas.com

---

**Version**: 1.0 | **Updated**: Nov 24, 2025

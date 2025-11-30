# 📚 SDK Integration Manual (.NET + Node)

**Scope:** Detailed API usage for all supported languages.

---

## 1. .NET SDK (`PrimusSaaS.*`)

### 🆔 Identity Validator
**Namespace:** `PrimusSaaS.Identity.Validator`

| Method | Description |
| :--- | :--- |
| `AddPrimusIdentity(Action<PrimusIdentityOptions>)` | Registers authentication services, JWT bearers, and validation logic. |

**Advanced Usage:**
```csharp
builder.Services.AddPrimusIdentity(options => {
    options.Issuers = new[] {
        new IssuerConfig { Type = IssuerType.AzureAd, ... },
        new IssuerConfig { Type = IssuerType.Auth0, ... }
    };
    options.AllowAnonymous = false; // Force auth globally
});
```

### 🔔 Notifications
**Namespace:** `PrimusSaaS.Notifications`

| Method | Description |
| :--- | :--- |
| `SendAsync(string template, object recipient, object data)` | Sends a notification via the configured channel. |

**DI Registration:**
```csharp
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(...)
    .UseSendGrid(...) // Optional
    .UseFileTemplates("./Templates"));
```

---

## 2. Node.js SDK (`@primus-saas/*`)

*Note: Node SDK follows the same architectural patterns as .NET.*

### 📦 Installation
```bash
npm install @primus-saas/identity @primus-saas/logging
```

### 🆔 Middleware Usage (Express)

```javascript
const { primusAuth } = require('@primus-saas/identity');

app.use(primusAuth({
  issuers: [
    { type: 'auth0', authority: '...', audience: '...' }
  ]
}));

app.get('/protected', (req, res) => {
  res.json({ user: req.user });
});
```

### 🪵 Logging Usage

```javascript
const { logger } = require('@primus-saas/logging');

logger.info('User logged in', { userId: '123' });
```

---

## 3. ⚠️ Common Pitfalls

1.  **Missing `app.UseAuthentication()`:** In .NET, this must be called before `UseAuthorization()`.
2.  **Environment Variable Naming:** .NET uses `__` (double underscore) for nesting (e.g., `PrimusIdentity__Issuers__0__Type`).
3.  **Template Paths:** Ensure your `NotificationTemplates` folder is copied to the output directory (`CopyToOutputDirectory="Always"`).

# 📦 Primus SaaS — Module Deep Dive

**Version:** 1.0  
**Status:** 🟢 Active  
**Scope:** Identity, Logging, Notifications

---

## 1. 🆔 Identity Validator Module

**Package:** `PrimusSaaS.Identity.Validator`  
**Purpose:** Secure, multi-provider authentication validation for .NET APIs.

### 🧠 Core Logic
The Identity module acts as a middleware gatekeeper. It intercepts HTTP requests and validates the `Authorization: Bearer <token>` header against a configured list of trusted issuers.

### ⚙️ Configuration Schema (`PrimusIdentity`)

| Field | Type | Description |
| :--- | :--- | :--- |
| `Issuers` | `Array` | List of trusted token providers. |
| `Issuers[].Type` | `Enum` | `AzureAd`, `Auth0`, `Local`, `Cognito`. |
| `Issuers[].Authority` | `Uri` | The URL of the OIDC provider (e.g., `https://login.microsoftonline.com/...`). |
| `Issuers[].Audience` | `String` | The expected `aud` claim (usually the API Client ID). |

### 🔍 Debugging Flow
When a token fails validation, the module logs metadata (never the token itself):
1.  **Issuer Mismatch:** Token `iss` does not match any config.
2.  **Audience Mismatch:** Token is for a different app.
3.  **Signature Invalid:** Key rotation or tampering detected.
4.  **Expired:** `exp` claim is in the past.

---

## 2. 🪵 Logging & Observability Module

**Package:** `PrimusSaaS.Logging`  
**Purpose:** Structured, privacy-aware logging with automatic context enrichment.

### 🧠 Core Logic
Replaces standard `Console.WriteLine` with structured JSON events. It automatically enriches logs with:
*   **Correlation ID:** Traces requests across microservices.
*   **User Context:** Adds `UserId` and `TenantId` if the user is authenticated.
*   **Environment:** Adds `Production`, `Staging`, etc.

### 🛡️ PII Protection
The module includes a **Redaction Engine** that scans log messages for patterns like:
*   Email Addresses
*   Credit Card Numbers
*   Social Security Numbers
*   Passwords

These are replaced with `[REDACTED]` before writing to the sink (Console, File, Application Insights).

---

## 3. 🔔 Notifications Module

**Package:** `PrimusSaaS.Notifications`  
**Purpose:** Channel-agnostic communication engine using Liquid templates.

### 🧠 Core Logic
Decouples the "intent" to send a message from the "implementation" of delivery.
*   **Input:** `SendAsync("PasswordReset", userObject, dataObject)`
*   **Process:**
    1.  Locate template: `NotificationTemplates/PasswordReset/{Channel}.liquid`
    2.  Render template using `userObject` + `dataObject`.
    3.  Dispatch via configured provider (SMTP, SendGrid, Twilio).

### 📝 Template System (Liquid)

Templates are file-system based for easy editing without recompiling.

**Directory Structure:**
```text
NotificationTemplates/
├── PasswordReset/
│   ├── EmailSubject.liquid
│   ├── EmailBody.liquid
│   └── SmsBody.liquid
└── WelcomeEmail/
    ├── EmailSubject.liquid
    └── EmailBody.liquid
```

**Standard Variables:**
*   `{{ recipient.email }}`
*   `{{ recipient.name }}`
*   `{{ data.code }}` (Dynamic payload)
*   `{{ app.url }}` (From config)

---

## 4. 🔮 Future Modules (Planned)

*   **Primus.Tenancy:** Multi-tenant data isolation strategies.
*   **Primus.Billing:** Stripe/Paddle integration wrappers.
*   **Primus.FeatureFlags:** Remote configuration and A/B testing.

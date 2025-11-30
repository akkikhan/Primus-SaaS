# 🔭 Logging & Observability Handbook

**Philosophy:** "Logs are data, not text."  
**Standard:** Structured Logging (Serilog/OpenTelemetry)

---

## 1. 📝 The Golden Rule of Logging

> **Never log PII (Personally Identifiable Information) or Secrets.**

**Forbidden Items:**
❌ Passwords / API Keys  
❌ JWT Tokens (Access/Refresh)  
❌ Credit Card Numbers  
❌ Full Names / Emails (unless explicitly audited and secured)

**Allowed Items:**
✅ User IDs (GUIDs)  
✅ Correlation IDs  
✅ Error Codes  
✅ Performance Metrics (Time taken)

---

## 2. 🏷️ Correlation & Tracing

Every request must have a `CorrelationId`. This ID flows through the system to link logs across different modules.

*   **HTTP Header:** `X-Correlation-ID`
*   **Log Property:** `CorrelationId`

**Example Flow:**
1.  Frontend generates `123-abc`.
2.  API receives request, attaches `CorrelationId: 123-abc` to logging context.
3.  API calls Database. Database logs include `123-abc`.
4.  API sends Email. Notification logs include `123-abc`.

---

## 3. 📊 Log Levels

| Level | Usage | Example |
| :--- | :--- | :--- |
| **Verbose** | Deep internal state. Disabled in Prod. | `Payload size: 405 bytes` |
| **Debug** | Flow control logic. Disabled in Prod. | `Entering method ValidateToken` |
| **Information** | Key business events. **Default for Prod.** | `Order #555 created by User #99` |
| **Warning** | Recoverable errors / Unexpected input. | `Login failed: Invalid password` |
| **Error** | Exceptions / System failures. | `Database connection timeout` |
| **Fatal** | App crash. | `Out of memory exception` |

---

## 4. 🚨 Alerting Thresholds

Configure your APM (Application Performance Monitor) to alert on:

1.  **Error Rate Spike:** > 1% of requests returning 5xx.
2.  **Latency Spike:** p95 response time > 2 seconds.
3.  **High Login Failures:** Potential brute-force attack.
4.  **Notification Failures:** SMTP/Gateway down.

---

## 5. 🛠️ Configuration

```json
"PrimusLogging": {
  "MinimumLevel": "Information",
  "EnableConsole": true,
  "EnableFile": false,
  "Redaction": {
    "MaskEmails": true,
    "MaskCreditCards": true
  }
}
```

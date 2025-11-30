# 🌟 Golden Path Playbook

**Definition:** The "Golden Path" is the supported, opinionated, and fastest way to build a feature using Primus.

---

## 1. 🛡️ Secure API Golden Path

**Goal:** Create a secure microservice.

1.  **Project:** `dotnet new webapi`
2.  **Package:** `PrimusSaaS.Identity.Validator`
3.  **Config:**
    *   Use `.env` for secrets.
    *   Bind `PrimusIdentity` section.
4.  **Code:**
    *   `AddPrimusIdentity()`
    *   `RequireAuthorization()` on endpoints.
5.  **Validation:**
    *   `curl -H "Authorization: Bearer <valid_token>"` returns 200.
    *   `curl` (no token) returns 401.

---

## 2. 📨 Transactional Email Golden Path

**Goal:** Send a welcome email.

1.  **Templates:**
    *   Create `NotificationTemplates/Welcome/EmailBody.liquid`.
    *   Content: `<h1>Welcome {{ recipient.name }}!</h1>`.
2.  **Package:** `PrimusSaaS.Notifications`
3.  **Code:**
    *   Inject `INotificationService`.
    *   `await _notifications.SendAsync("Welcome", user, new {});`
4.  **Validation:**
    *   Check SMTP inbox (or MailTrap/Papercut in dev).

---

## 3. 📜 Observability Golden Path

**Goal:** Trace a request from start to finish.

1.  **Package:** `PrimusSaaS.Logging`
2.  **Code:**
    *   `builder.Logging.AddPrimus(...)`
3.  **Usage:**
    *   Do not use `Console.WriteLine`.
    *   Use `_logger.LogInformation("Processing order {OrderId}", orderId)`.
4.  **Validation:**
    *   Verify `CorrelationId` is present in the output JSON.

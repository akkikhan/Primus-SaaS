# ✅ Golden Path Validation Checklist

**Purpose:** Manual verification steps before any release.

---

## 1. 🆔 Identity Module

- [ ] **NuGet Install:** Can install `PrimusSaaS.Identity.Validator` into a fresh project?
- [ ] **Startup:** App starts without errors with minimal config?
- [ ] **Token Valid:** `curl` with valid token returns 200?
- [ ] **Token Invalid:** `curl` with bad signature returns 401?
- [ ] **Logs:** No full tokens visible in console logs?

---

## 2. 🪵 Logging Module

- [ ] **NuGet Install:** Can install `PrimusSaaS.Logging`?
- [ ] **Console Output:** Logs appear in JSON format?
- [ ] **Correlation:** `CorrelationId` is present?
- [ ] **PII:** "user@example.com" is redacted to "[REDACTED]"?

---

## 3. 🔔 Notifications Module

- [ ] **NuGet Install:** Can install `PrimusSaaS.Notifications`?
- [ ] **Templates:** `NotificationTemplates` folder copied to output?
- [ ] **Send:** `SendAsync` completes successfully?
- [ ] **Delivery:** Email actually arrives (or hits mock server)?

---

## 4. 🎭 Live UI Demo

- [ ] **Build:** `LiveDemoApi` and `LiveDemoFrontend` build clean?
- [ ] **Run:** `npm start` / `dotnet run` works?
- [ ] **Interaction:** Can click through all demo tiles without console errors?

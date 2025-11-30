# 🧪 End-to-End (E2E) Test Plan

**Scope:** Full platform validation (SDKs + Live Demo).  
**Tools:** Playwright (UI), xUnit (Integration), K6 (Load).

---

## 1. 🎯 Objectives

*   Verify that all modules function correctly in a production-like environment.
*   Ensure no regression in "Golden Paths".
*   Validate cross-module compatibility (e.g., Identity + Logging working together).

---

## 2. 🏗️ Test Environment

*   **App:** `LiveDemoApi` + `LiveDemoFrontend` (Dockerized).
*   **Config:** `appsettings.Testing.json` + Environment Variables.
*   **Mocking:**
    *   **Identity:** Use a "Local" issuer with a known signing key.
    *   **Email:** Use a mock SMTP server (e.g., MailHog) to capture emails.

---

## 3. 🚦 E2E Scenarios (High Priority)

### Scenario A: User Authentication Flow
1.  **Action:** User clicks "Login" on Frontend.
2.  **System:** Redirects to Auth Provider -> Callback -> Token Exchange.
3.  **Validation:**
    *   Frontend receives valid JWT.
    *   Frontend can call protected API `/whoami`.
    *   API logs the event with `UserId`.

### Scenario B: Password Reset Flow
1.  **Action:** User requests password reset for `test@example.com`.
2.  **System:** Generates token -> Renders Template -> Sends Email.
3.  **Validation:**
    *   Mock SMTP receives email.
    *   Email body contains the correct reset link.
    *   Link is clickable and leads to the correct UI page.

---

## 4. 🤖 Automation Strategy

We use **Playwright** for UI-driven E2E tests.

```typescript
// example.spec.ts
test('should log in and see dashboard', async ({ page }) => {
  await page.goto('http://localhost:4200');
  await page.click('#login-btn');
  // ... auth flow ...
  await expect(page.locator('#dashboard-welcome')).toContainText('Welcome');
});
```

---

## 5. 🚫 Defect Severity

*   **Sev 1 (Blocker):** Golden Path fails. Cannot login or send email.
*   **Sev 2 (Critical):** PII leak in logs.
*   **Sev 3 (Major):** UI glitch in Live Demo.
*   **Sev 4 (Minor):** Typo in documentation.

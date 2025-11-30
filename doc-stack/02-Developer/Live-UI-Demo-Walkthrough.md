# 🎭 Live UI Demo Walkthrough

**URL:** `http://localhost:4200` (Local) / `https://demo.primus.com` (Prod)  
**Purpose:** The "Source of Truth" for platform capabilities.

---

## 1. 🧭 Navigation Structure

The Live Demo is organized by **Module**.

*   **🏠 Dashboard:** Overview of installed modules and system health.
*   **👤 Identity:** Login flows, token inspection, multi-provider testing.
*   **🔔 Notifications:** Template preview, send test email/SMS, history log.
*   **🪵 Logging:** Live log stream (simulated), PII redaction demo.

---

## 2. 👤 Identity Demo

**Scenario:** User logs in with Auth0.
1.  Click **"Login with Auth0"**.
2.  Redirects to Universal Login.
3.  Returns to `/callback`.
4.  **Demo Panel Shows:**
    *   **Raw Token:** (Truncated)
    *   **Decoded Claims:** `sub`, `iss`, `aud`, `exp`.
    *   **Validation Status:** ✅ Valid
    *   **Backend Check:** Calls `/api/identity/whoami` to prove the backend accepts the token.

---

## 3. 🔔 Notifications Demo

**Scenario:** Send a "Password Reset" email.
1.  Go to **Notifications > Send Test**.
2.  **Select Template:** `PasswordReset`.
3.  **Enter Recipient:** `test@example.com`.
4.  **Enter Data:** Code = `123456`.
5.  Click **Send**.
6.  **Result:**
    *   UI shows "Queued".
    *   "Preview" tab shows the rendered HTML (Liquid result).
    *   Backend logs show "Email sent to test@example.com".

---

## 4. 🪵 Logging Demo

**Scenario:** Visualize PII Redaction.
1.  Go to **Logging > Generator**.
2.  Type a message: `My password is secret123`.
3.  Click **Log Information**.
4.  **Result:**
    *   Log Console shows: `My password is [REDACTED]`.
    *   Demonstrates the safety of the logging pipeline.

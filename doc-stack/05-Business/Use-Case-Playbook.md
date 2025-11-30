# 📖 Use Case Playbook

**Target Industries:** FinTech, HealthTech, Enterprise SaaS.

---

## 1. 🏥 HealthTech (HIPAA Compliance)

**Challenge:** Strict logging requirements. Cannot log patient names or IDs.
**Primus Solution:**
*   Use **Primus.Logging** with strict Redaction policies.
*   Configure `MaskRegex` for SSN and MRN (Medical Record Number).
*   **Result:** Compliant logs out of the box.

---

## 2. 💳 FinTech (Security First)

**Challenge:** Must support multiple banks (tenants) with different Auth providers (Azure AD for Bank A, Okta for Bank B).
**Primus Solution:**
*   Use **Primus.Identity** Multi-Issuer support.
*   Configure `Issuers` array: `[ {Type: AzureAd, ...}, {Type: Okta, ...} ]`.
*   **Result:** Single API handles users from all banks securely.

---

## 3. 🛒 E-Commerce (High Volume)

**Challenge:** Sending millions of order confirmations without blocking the checkout flow.
**Primus Solution:**
*   Use **Primus.Notifications**.
*   Call `SendAsync` (fire-and-forget).
*   Swap providers easily (e.g., switch from SendGrid to SES) via config if costs get too high.
*   **Result:** Scalable, non-blocking notifications.

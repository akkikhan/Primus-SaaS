# 🎬 The "Real-Life" Demo Script (Acme Dashboard)

**Scenario:** "We are Acme Corp. We have a new financial dashboard, but it's currently insecure. We need to integrate Primus SaaS to protect our data."

---

## 🛠️ PRE-MEETING PREP

1.  **Terminal 1 (Portal)**: Ensure your Primus Portal Backend & Frontend are running.
2.  **Terminal 2 (Demo App)**:
    ```bash
    cd test-apps/acme-dashboard
    npm install
    node server.js
    ```
3.  **Browser**: Open `http://localhost:3000` (The Acme Login Page).

---

## 🎥 LIVE DEMO START

### Phase 1: The Problem (Visual)
*Narrative: "Here is our new application. It has a login page, but the backend isn't connected to our identity provider yet."*

1.  **Login**:
    *   Enter `admin@primussaas.com` / `Admin123!`
    *   Click **Sign In**.
2.  **The Failure**:
    *   You will be redirected to the Dashboard.
    *   **Result:** You see a big ⚠️ **Access Denied** screen.
    *   *Read the error:* "Backend rejected the token. Reason: Security Module Not Configured."
    *   *Say:* "The application has the token, but the backend server doesn't trust it yet because we haven't installed the security policy."

---

### Phase 2: The Solution (Portal)
*Narrative: "Let's fix this in the Primus Portal."*

1.  **Create App**:
    *   Go to `http://localhost:5173` (Primus Portal).
    *   Create New App -> Name: "Acme Financials".
2.  **Assign Module**:
    *   Assign `IdentityValidator`.
3.  **Get Keys**:
    *   Copy the **Client ID** and **Client Secret**.

---

### Phase 3: The Integration (Code)
*Narrative: "Now we simply paste these keys into our backend configuration."*

1.  **Open Code**:
    *   Open `test-apps/acme-dashboard/server.js`.
2.  **Paste Keys**:
    *   Find the `PRIMUS_CONFIG` object (lines 16-17).
    *   Replace `'REPLACE_WITH_CLIENT_ID'` and `'REPLACE_WITH_SECRET'`.
3.  **Restart Server**:
    *   Go to terminal -> `Ctrl+C` -> `node server.js`.

---

### Phase 4: The Success (Visual)
*Narrative: "Now that the backend is secured with Primus, let's try again."*

1.  **Reload Dashboard**:
    *   Go back to `http://localhost:3000/dashboard.html`.
    *   Click **Retry** (or refresh).
2.  **Result**:
    *   The error disappears.
    *   **Success:** The Revenue, Growth, and User numbers appear!
    *   *Say:* "And just like that, the application is securely validating users and serving data."

---

## 🧠 Phase 5: Explaining the Flow (Architecture)
*Narrative: "So, what just happened under the hood?"*

1.  **The Token Flow**:
    *   "The frontend (Acme Dashboard) authenticated the user and received a **JWT Token**."
    *   "It sent this token to the backend in the API request."

2.  **The Primus Guard**:
    *   "Our SDK (`primus-identity-validator`) intercepted that request."
    *   "It validated the token signature locally using the keys we just configured."
    *   "It ensured the user had the right permissions."

3.  **Azure AD / Hybrid Support**:
    *   *Say:* "If we were using Azure AD, the flow is identical for the developer."
    *   "The frontend would get a token from Microsoft."
    *   "Our SDK would automatically fetch Microsoft's public keys to validate it."
    *   "This means you can switch from Local Auth to Enterprise SSO by just changing one line of config (`mode: 'AzureAd'`)."

---

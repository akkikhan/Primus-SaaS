# 🏛️ Primus Authentication Architecture V2 (The "Ferrari" Model)

**Status**: Draft / Approved Plan  
**Philosophy**: Deterministic, Secure, Enterprise-Grade. No "mystery meat" tokens.

---

## 🚀 The Three Modes Defined

### 1️⃣ Mode 1: Azure AD (External IdP)
**"The Standard Flow"**

*   **Issuer**: Microsoft Azure AD (`https://login.microsoftonline.com/...`)
*   **Token**: OIDC/OAuth2 Access Token (JWT).
*   **Flow**:
    1.  User clicks "Login with Microsoft" in Client App.
    2.  Redirects to Azure AD -> User authenticates.
    3.  Azure AD returns Access Token to Client App.
    4.  Client App sends Token to API.
    5.  **Primus SDK** validates signature using Azure AD Public Keys (JWKS).
*   **Role**: Primus is purely a **Validator**.

### 2️⃣ Mode 2: Local Mode (Primus as IdP)
**"The Managed Identity Provider"**

*   **Issuer**: Primus SaaS Portal (`https://primus-portal.com`)
*   **Token**: Primus-Signed JWT.
*   **Flow**:
    1.  User clicks "Login" in Client App.
    2.  Client App calls Primus Portal API: `POST /api/v1/auth/login`.
    3.  Primus Portal verifies credentials (hashed in Primus DB).
    4.  Primus Portal **issues** a JWT signed with its own Private Key.
    5.  Client App sends Token to API.
    6.  **Primus SDK** validates signature using Primus Public Key (or Shared Secret).
*   **Role**: Primus is the **Issuer** AND **Validator**.
*   **Requirement**: Primus Portal must have a User Store and Token Service.

### 3️⃣ Mode 3: Hybrid Mode (Dual Identity)
**"The Choice, Not the Fallback"**

*   **Issuer**: Either Microsoft OR Primus.
*   **Flow**:
    1.  Client App offers two buttons: "Login with Microsoft" OR "Login with Local".
    2.  **Scenario A (Azure)**: User chooses Microsoft -> Gets Azure Token.
    3.  **Scenario B (Local)**: User chooses Local -> Gets Primus Token.
    4.  Client App sends Token to API.
    5.  **Primus SDK** inspects the `iss` (Issuer) claim:
        *   If `iss` contains `microsoft` -> Route to **AzureAdValidator**.
        *   If `iss` matches Primus -> Route to **LocalValidator**.
*   **Role**: Primus SDK acts as a **Smart Router** based on the token's origin.

---

## 🛠️ Implementation Roadmap

### **Phase 1: Azure AD (✅ Complete)**
- SDK validates Azure tokens.
- Portal generates config.
- E2E verified.

### **Phase 2: Local Mode (🚧 To Do)**
- **Portal Backend**:
    - Create `Users` table for Client Apps (distinct from Portal Admins).
    - Implement `POST /auth/login` endpoint.
    - Implement JWT Signing Service (using RSA/HS256).
- **SDK**:
    - Update `LocalValidator` to verify Primus signatures.
    - (Optional) Add `login()` helper method to SDK.

### **Phase 3: Hybrid Mode (🚧 To Do)**
- **SDK Refactor**:
    - Remove "Fallback" logic (Try/Catch).
    - Implement "Issuer Switching" logic.
    - `validateToken(token)` -> `decode(token)` -> `check issuer` -> `dispatch`.

---

## 📊 Comparison

| Feature | Azure AD Mode | Local Mode | Hybrid Mode |
|---------|---------------|------------|-------------|
| **Token Source** | Azure AD | **Primus Portal** | Both |
| **User Store** | Azure AD | **Primus DB** | Both |
| **Validation** | JWKS (Public Key) | Shared Secret / Public Key | **Smart Switch** |
| **Primary Use** | Enterprise / B2B | SMEs / Internal Apps | Transition / Flex |

---

**This architecture ensures Primus is never "guessing" about a token. Every token has a clear, trusted issuer.**

# Token Refresh Scope

Primus Identity Validator focuses on token validation and diagnostics. It does **not** implement refresh-token issuance or storage. Use your IdP (Azure AD, Auth0, Okta, etc.) to handle refresh/renewal flows.

Recommended patterns:
- For Azure AD: use MSAL on the client to acquire/refresh tokens silently.
- For backend-to-backend: use client credentials and cache tokens in your service.
- Keep access tokens short-lived; rely on IdP refresh.

If you need a custom refresh mechanism, implement it outside this library (e.g., dedicated auth service) to avoid mixing concerns and security foot-guns.

# Trunked Primus Frontend Sample

This React + Vite app drives the sample backend (`examples/trunked-npm-backend`) that already integrates the published [`primus-identity-validator`](https://www.npmjs.com/package/primus-identity-validator) middleware. The UI mimics a production dashboard so you can demonstrate:

1. How the application looked before authentication (set `PRIMUS_ENFORCE_AUTH=false` on the backend).
2. How the exact same code paths require Primus / Azure AD tokens as soon as the middleware is enabled.

## Setup

1. Install dependencies (already done when scaffolding, but safe to repeat):

   ```bash
   npm install
   ```

2. Copy the environment template and set the backend URL if it differs from the default `http://localhost:4000`:

   ```bash
   cp .env.example .env
   ```

3. In a separate terminal, start the backend sample and ensure it has valid Primus credentials configured. Flip `PRIMUS_ENFORCE_AUTH` to show the “before” (open) vs “after” (secured) experience.

4. Start the frontend dev server:

   ```bash
   npm run dev
   ```

   Visit the printed Vite URL (usually http://localhost:5173).

## Using the UI

- The hero cards explain the “Before Integration” (public) vs “After Integration” (Primus-enforced) states, mirroring what your backend is currently doing.
- Paste a Primus Portal or Azure AD access token into the **Access Token** box. Tokens are obtained exactly the way the npm README describes (Primus login flow or `az account get-access-token` for Azure AD).
- Use **Test Public Endpoint** to show that basic health checks remain unauthenticated even after integration.
- Use **Load Dashboard Data** to fetch the protected `/api/dashboard` + `/api/notifications` endpoints. Without valid tokens (or when `PRIMUS_ENFORCE_AUTH=false`) the cards will either load or stay locked, demonstrating the immediate impact of wiring the npm middleware.
- When Primus authentication succeeds, the banner at the bottom displays the signed-in user to confirm which identity was validated.

This keeps both halves of the sample focused on verifying the documented integration path without introducing additional auth helpers that could hide SDK issues.

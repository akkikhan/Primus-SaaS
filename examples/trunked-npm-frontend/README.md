# Trunked Primus Frontend Sample

This React + Vite app drives the sample backend (`examples/trunked-npm-backend`) that already integrates the published [`primus-identity-validator`](https://www.npmjs.com/package/primus-identity-validator) middleware. Use the UI to paste JWT tokens and test each protected route without writing curl commands.

## Setup

1. Install dependencies (already done when scaffolding, but safe to repeat):

   ```bash
   npm install
   ```

2. Copy the environment template and set the backend URL if it differs from the default `http://localhost:4000`:

   ```bash
   cp .env.example .env
   ```

3. In a separate terminal, start the backend sample and ensure it has valid Primus credentials configured.

4. Start the frontend dev server:

   ```bash
   npm run dev
   ```

   Visit the printed Vite URL (usually http://localhost:5173).

## Using the UI

- Paste a Primus Portal or Azure AD access token into the **Access Token** box. Tokens are obtained exactly the way the npm README describes (Primus login flow or `az account get-access-token` for Azure AD).
- Click any of the buttons to call the backend routes:
  - `Health` and `Public` require no token and confirm connectivity.
  - `Profile`, `Admin`, and `Management` require Bearer tokens and map to the middleware-protected routes you configured on the backend.
- Responses (including HTTP status codes) are rendered verbatim so you can copy them into docs or bug reports if behaviour diverges from the npm instructions.

This keeps both halves of the sample focused on verifying the documented integration path without introducing additional auth helpers that could hide SDK issues.

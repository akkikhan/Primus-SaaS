# Trunked Primus Backend Sample

This folder contains a minimal Express backend that consumes the published [`primus-identity-validator`](https://www.npmjs.com/package/primus-identity-validator) package exactly the way it is documented on npm. Use it as a quick start template for wiring the validator into any trunked npm service.

## Prerequisites

- Node.js 18+ (the SDK targets modern Node runtimes)
- Valid Primus SaaS portal credentials plus the secrets that match the validation mode you plan to use

## Getting Started

1. Install dependencies:

   ```bash
   npm install
   ```

2. Copy the sample environment file and fill in your real values:

   ```bash
   cp .env.example .env
   ```

   Required variables depend on the validation mode that the SDK supports (Local, AzureAd, or Hybrid). These flags exactly mirror the table in the npm README:

   | Mode     | Required Secrets                               |
   |----------|------------------------------------------------|
   | Local    | `PRIMUS_JWT_SECRET`                            |
   | AzureAd  | `PRIMUS_AZURE_TENANT_ID`                       |
   | Hybrid   | Both `PRIMUS_JWT_SECRET` and `PRIMUS_AZURE_TENANT_ID` |

   All modes require `PRIMUS_PORTAL_URL`, `PRIMUS_CLIENT_ID`, and `PRIMUS_CLIENT_SECRET`.

3. (Optional) Launch the paired React frontend located at `examples/trunked-npm-frontend` after you start this backend. The UI simply drives the sample endpoints so you can try each validation scenario without crafting curl commands.

4. Start the API:

   ```bash
   npm run dev          # hot reload with tsx
   npm run build && npm start   # compile to dist/ and run with node
   ```

## What the sample does

- Registers `primusIdentityMiddleware` using the exact configuration object from the npm docs.
- Demonstrates Local / Azure AD / Hybrid flows by switching `PRIMUS_VALIDATION_MODE`.
- Exposes routes that align with the usage snippets from the package README:
  - `GET /health` – Shows which validation mode is active and whether Local/Azure requirements are enabled.
  - `GET /api/public` – Open endpoint for smoke testing the server.
  - `GET /api/profile` – Protected endpoint that returns `req.primusUser`.
  - `GET /api/admin` – Requires the `Admin` role via `requireRoles`.
  - `GET /api/management` – Accepts either `Manager` or `Admin` roles.

The route handlers intentionally avoid any additional business logic so you can copy/paste them into your own module.

## Obtaining tokens for testing

Follow the instructions in the npm README:

```bash
# For Azure AD tokens using Azure CLI
az account get-access-token --resource "api://your-client-id" --query accessToken -o tsv
```

Or request a Primus Portal token via the standard login flow and send it in the `Authorization: Bearer <token>` header when calling the protected endpoints.

## Notes

- This sample does **not** patch, shim, or modify the SDK. If you notice behavioural issues, document them separately instead of fixing them here—per the request, we want the integration to stay faithful to the published package.
- `src/types/express.d.ts` augments the Express request type with `primusUser` so the TypeScript compiler understands what the middleware attaches.

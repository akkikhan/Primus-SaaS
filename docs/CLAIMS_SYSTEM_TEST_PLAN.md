# Integration Test & Quality Blueprint

This blueprint documents how to exercise the identity validator and structured logging packages in a neutral web application scenario. It focuses on validating tokens, enriching logs, and ensuring the middleware stack behaves consistently across environments.

## Scope & Components
- **Identity enforcement**: `@primus-saas/identity-validator` validates both OIDC (Azure AD) and locally signed JWT tokens and attaches `req.primusUser` for downstream consumers.
- **Logging/Observability**: `@primus-saas/logging` produces structured logs with `requestId`, HTTP metadata, duration, and user context once authentication succeeds. Middleware wiring lives in `test-apps/acme-dashboard/server.js`.
- **Sample API**: Public health endpoint and protected resource endpoints that reflect typical read/write operations. No role-based logic is included; access is binary (authenticated vs unauthenticated).

## Key Paths in Code
- `test-apps/acme-dashboard/server.js`: Express bootstrap showing middleware order (logging first, then identity, then routes). Exports `app` for tests.
- `test-apps/acme-dashboard/__tests__/app.test.js`: Jest + Supertest coverage for public vs protected routes, valid-token success, and audience rejection.
- SDK references: `sdk/nodejs/primus-identity-validator/README.md`, `sdk/logging/nodejs/src/middleware/express.ts`, `sdk/logging/nodejs/examples/express-integration.js`.

## How to Run
```bash
cd test-apps/acme-dashboard
npm install        # already executed once in this repo
npm test           # runs Jest in-band
npm start          # start the server locally on PORT (default 3000)
```

## Test Coverage (Current)
- **Public health**: `/api/health` is open and returns 200 with a status payload.
- **Auth enforcement**: `/api/data` rejects missing tokens (401).
- **Happy path (local JWT)**: Valid HS256 token (issuer `LOCAL_ISSUER`, audience `API_AUDIENCE`) returns 200 with the decoded identity; logs include `requestId` and user context.
- **Audience failure**: Token signed correctly but wrong audience → 401 with an audience error.

## Additional Test Matrix (Add as needed)
- **OIDC token**: Real Azure AD token (matching `AZURE_AD_ISSUER`/`AUTHORITY` and audience) returns 200 using the same middleware configuration.
- **Expiry/nbf handling**: Expired or not-yet-valid tokens → 401; verify clock-skew tolerance configured in middleware.
- **JWKS rotation**: When JWKS URLs are used, rotate signing keys and confirm cache refresh accepts new keys without downtime.
- **CORS**: Successful preflight OPTIONS for approved origins; rejected for disallowed origins.
- **Error paths**: Upstream dependency failure (e.g., data store) returns 500 with structured logs that omit sensitive fields.
- **Performance**: Measure P95/P99 latency with identity + logging enabled to ensure the middleware stays lightweight.
- **Security hygiene**: HTTPS enforced, secrets never logged, tokens kept out of query strings.

## Happy / Smooth / Rough / Current
- **Happy**: Valid token from a trusted issuer with a matching audience. Logs carry `requestId`, `userId`/`email`, status, and duration; the health check stays green.
- **Smooth**: Middleware order remains logging → identity → routes. Configuration comes from environment variables. Logger minimum level stays at `INFO` (or `WARN`) in production setups.
- **Rough edges to watch**:
  - Audience or issuer mismatch → 401 (most common support issue).
  - Token expiry/clock skew (default 300 seconds) causing unexpected 401s if client clocks drift.
  - JWKS cache refresh gaps during key rollover.
  - Frontend token storage: avoid localStorage when possible; prefer httpOnly cookies or in-memory storage.
  - CORS misconfiguration blocking legitimate requests.
  - PII leaking into logs if additional context objects contain raw payloads.
- **Current state in repo**:
  - Identity + logging middleware wired with user context flowing into logs post-authentication.
  - Jest + Supertest tests cover public vs protected routes, valid token flow, and audience rejection.
  - Server exports `app` for tests and only binds to a port when run directly.

## Nuances That Commonly Break
- Incorrect audience/client ID alignment between Azure app registration and server configuration.
- Missing `Bearer` prefix or incorrect header casing on requests.
- Local JWT secret rotated without updating environment variables.
- JWKS endpoint outages leading to authentication failures; monitor and retry.
- OPTIONS preflight denied when Authorization headers are required but not allowed in CORS policy.

## Recommended Next Additions
- Add end-to-end tests that exercise real OIDC tokens once a tenant is available.
- Add a CI workflow to run `npm test` on push or PR.
- Configure a log target (file, collector, or hosted logging) for environments where console scraping is insufficient.
- Document advanced logging enrichers or masking strategies as they are implemented.

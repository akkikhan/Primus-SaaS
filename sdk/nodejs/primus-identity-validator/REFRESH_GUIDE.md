# Token Refresh Guide (Node.js)

The Node.js SDK is a validator-only library, mirroring the NuGet surface but without issuing tokens.

## Recommended approach
- Let your Identity Provider (Azure AD, Auth0, etc.) issue refresh tokens.
- Use your IdP’s SDK on the client to exchange refresh tokens for new access tokens.
- The validator simply enforces issuer/audience/signature/lifetime on the new access token.

## Why no built-in refresh?
- Token issuance is IdP-specific and out of scope for a pure validator.
- Keeps the Node package aligned with the NuGet validator responsibilities.

## Handling refresh failures
- If the client cannot refresh, expect 401 until a valid access token is presented.
- Pair with the logging middleware to capture failed refresh attempts and correlate with request IDs.

# Secret Management (Node.js)

Aligns with the NuGet guidance: never store secrets in code or source control.

## Recommendations
- Use environment variables for `secret` and audience values in local development.
- For production, use a managed store (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault).
- Do not commit `.env` files; add them to `.gitignore`.
- Rotate secrets regularly and keep rotation windows short.

## Example (.env)

```
JWT_SECRET=super-long-random-key
JWT_ISSUER=https://auth.local
JWT_AUDIENCE=api://my-api
```

## Wiring secrets into the middleware

```typescript
primusIdentityMiddleware({
  issuers: [{
    name: 'LocalAuth',
    type: 'jwt',
    issuer: process.env.JWT_ISSUER!,
    secret: process.env.JWT_SECRET!,
    audiences: [process.env.JWT_AUDIENCE!]
  }]
});
```

## Azure AD notes
- `authority` and `issuer` must be HTTPS in production (`requireHttpsMetadata` defaults to `true`).
- Do not hardcode client secrets in Node services; fetch them at startup.

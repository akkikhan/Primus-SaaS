# Production Deployment Guide

Best practices for deploying the Identity Validator in a production environment.

## Security Checklist

- [ ] **Use HTTPS**: Always run your application over HTTPS. The validator requires HTTPS metadata by default.
- [ ] **Secure Secrets**: Never hardcode secrets (for JWT issuers) in your code. Use environment variables or a secret manager (Azure Key Vault, AWS Secrets Manager).
- [ ] **Restrict Audiences**: Only allow audiences that strictly match your application. Do not use wildcards.
- [ ] **Short Token Lifetimes**: Configure your identity provider to issue short-lived access tokens (e.g., 1 hour) and use refresh tokens.

## Performance Optimization

- **Caching**: The validator automatically caches OIDC discovery documents and JWKS keys. Ensure your application has enough memory for this cache.
- **Clock Skew**: Keep `ClockSkew` small (e.g., 5 minutes or less) to minimize the window for replay attacks.

## Azure AD Specifics

When using Azure AD:
1. **V2 Endpoint**: We recommend using the V2 endpoint (`.../v2.0`).
2. **App ID URI**: Use the Application ID URI (e.g., `api://<client-id>`) as your audience, not just the Client ID.
3. **Multi-Tenant**: If supporting multi-tenant, validate the `tid` (tenant ID) claim if you want to restrict access to specific tenants.

## Troubleshooting in Production

- **Logs**: Ensure you are capturing authentication logs.
- **Correlation IDs**: Use the Logging SDK to track requests across services.
- **Health Checks**: Implement a health check endpoint that verifies connectivity to your OIDC provider.

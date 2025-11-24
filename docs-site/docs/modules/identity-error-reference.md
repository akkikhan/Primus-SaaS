# Error Reference

Common errors you might encounter when using the Identity Validator and how to resolve them.

## 401 Unauthorized

### "IDX10501: Signature validation failed"
**Cause:** The token signature does not match the configured secret or public key.
**Solution:**
- For **JWT**: Ensure the `secret` in your config exactly matches the one used to sign the token.
- For **OIDC**: Ensure the `authority` URL is correct and the provider is publishing valid JWKS keys.

### "IDX10214: Audience validation failed"
**Cause:** The `aud` claim in the token does not match any value in your `audiences` configuration.
**Solution:** Check the token's `aud` claim (decode at jwt.io) and add it to your `audiences` list.

### "IDX10205: Issuer validation failed"
**Cause:** The `iss` claim in the token does not match the configured `issuer`.
**Solution:** Ensure the `issuer` in your config matches exactly what the token provider sends. Note that Azure AD V1 and V2 endpoints have different issuer formats.

### "The token is expired"
**Cause:** The `exp` claim is in the past (minus clock skew).
**Solution:** Generate a new token. Increase `ClockSkew` if servers have drifted time.

## 403 Forbidden

### "User does not have required roles"
**Cause:** The user is authenticated, but the endpoint requires a specific role that the user lacks.
**Solution:** Ensure the token contains the `roles` claim with the required value.

## Debugging Tips

1. **Inspect the Token**: Use [jwt.io](https://jwt.io) to decode your token and verify claims (`iss`, `aud`, `exp`).
2. **Check Logs**: Enable Debug logging in your application to see detailed validation steps.
   - **.NET**: Set `"Microsoft.AspNetCore.Authentication": "Debug"` in `appsettings.json`.
   - **Node.js**: Set `logLevel: 'debug'` in the validator config.

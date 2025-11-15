# Primus SaaS Platform – Frequently Asked Questions

**Version**: 1.1  
**Last Updated**: November 15, 2025

---

## General Questions

### Q: What is Primus SaaS Platform?

**A**: Primus SaaS Platform is a developer-focused platform that provides reusable backend modules (starting with authentication) as NuGet and NPM packages. The platform includes:

- **Portal**: Internal admin tool for managing module catalog and client applications
- **SDK Modules**: Production-ready packages for common backend features
- **Documentation**: Integration guides and code snippets for client developers

The first module is **Identity Validator** for authentication (Local + Azure AD).

---

### Q: Who uses Primus SaaS Platform?

**A**: There are three personas:

1. **Platform Admin** (internal): Manages the portal, module catalog, and generates integration docs
2. **Client Developer** (external): Integrates Primus modules into their backend applications
3. **End User** (external): Uses client applications; never interacts with Primus directly

In v1, only Platform Admins log into the portal. Client developers receive documentation via email/PDF.

---

### Q: Do client developers need to create accounts on Primus Portal?

**A**: No, not in v1. The Portal is an internal tool for Platform Admins only. Admins create application entries and share the generated Documentation with client developers through external channels (email, PDF, Confluence).

---

## Architecture & Data Flow

### Q: How does authentication work at runtime?

**A**: All authentication logic runs **inside the client's backend application**. Here's the flow:

**Azure AD Mode**:

1. Client's frontend redirects user to Azure AD
2. Azure AD issues token to frontend
3. Frontend calls client's backend API with token in `Authorization` header
4. Primus SDK (running in client's backend) validates token
5. SDK extracts user claims and passes to application logic

**Local Mode**:

1. Client's frontend sends username/password to client's backend
2. Backend validates credentials against client's database
3. Primus SDK generates JWT token
4. Token returned to frontend
5. Frontend includes token in subsequent API calls
6. SDK validates token on each request

**Key Point**: Primus SaaS Platform servers are never involved in these flows. The SDK runs as an in-process library.

---

### Q: Does Primus store or process user data or tokens?

**A**: **No**. Primus never sees:

- User credentials (usernames, passwords)
- Authentication tokens (local JWT or Azure AD tokens)
- User profile data
- Business data

Primus only stores:

- Application metadata (name, stack)
- Module usage info (which module, which version)
- `PrimusClientId` (application identifier)

All user data and tokens stay within the client's infrastructure.

---

### Q: What happens if Primus servers go down?

**A**: **Nothing**. Client applications continue to work normally because:

- Authentication logic runs in client's backend (SDK in-process)
- No runtime API calls to Primus servers
- Portal is only used for setup and management tasks

Primus downtime would only affect:

- Portal access for admins (can't create new apps or update catalog)
- Ability to generate new Documentation bundles

Already-integrated client applications are unaffected.

---

### Q: Can clients run Primus modules on-premises?

**A**: **Yes**. The SDK is just code (a NuGet/NPM package). Clients can:

- Install packages from public registries (NuGet.org, npmjs.com)
- Host their backend anywhere (cloud, on-prem, hybrid)
- Use any database for user storage
- Comply with any data residency requirements

Primus has no control over where client applications run.

---

## Module Updates & Versioning

### Q: How do client applications know when updates are available?

**A**: Through multiple channels:

1. **Portal Dashboard**: Admins see which applications are using outdated versions
2. **Email Notifications**: Portal generates update notifications that admins send to client developers
3. **GitHub Releases**: Clients can subscribe to release notifications on module repositories
4. **Package Registries**: Standard npm/NuGet tooling shows available versions

There's no automatic update mechanism. Clients explicitly upgrade when ready.

---

### Q: How do clients update to a new module version?

**A**: Using standard package management:

**.NET**:

```bash
dotnet add package Primus.SaaS.IdentityValidator --version 2.0.0
```

Or update `.csproj`:

```xml
<PackageReference Include="Primus.SaaS.IdentityValidator" Version="2.0.0" />
```

**Node**:

```bash
npm install @primus-saas/identity-validator@2.0.0
```

Or update `package.json`:

```json
{
  "dependencies": {
    "@primus-saas/identity-validator": "^2.0.0"
  }
}
```

Then follow any migration instructions provided in release notes (config changes, code updates).

---

### Q: Is there a special command to update Primus modules?

**A**: **No**. Primus modules are standard packages. Use normal `dotnet`, `npm`, or `yarn` commands. No custom updater or CLI tool is required.

---

### Q: What if a client doesn't want to upgrade?

**A**: That's fine. Clients control when to upgrade. Old versions continue to work (SDK runs in their infrastructure). However:

- Security patches should be applied promptly
- Breaking changes in major versions may require eventual upgrades
- Primus may deprecate very old versions (with long notice periods)

Admins can track which clients are on old versions via the Portal and follow up as needed.

---

## Security & Token Validation

### Q: Why do we need to validate tokens? Can't we just trust them?

**A**: **No, you must validate tokens**. Without validation, attackers can:

1. **Forge tokens**: Create fake JWT with elevated privileges (e.g., `role: "admin"`)
2. **Tamper with claims**: Modify user ID or permissions in token payload
3. **Reuse expired tokens**: Use old tokens indefinitely
4. **Replay tokens from other apps**: Use tokens meant for different applications
5. **Use the "none" algorithm**: Remove signature entirely

Validation ensures:

- Token was issued by trusted authority (Azure AD or your app)
- Token hasn't been tampered with (signature check)
- Token is still valid (expiry check)
- Token is meant for your app (audience check)

---

### Q: What exactly does the Primus SDK validate?

**A**: The SDK performs comprehensive validation:

**For Azure AD tokens**:

- ✅ Signature (using Azure AD's public keys from JWKS endpoint)
- ✅ Issuer (must be correct Azure AD tenant)
- ✅ Audience (must be your API's client ID)
- ✅ Expiry (token not expired)
- ✅ Not Before (token not used too early)
- ✅ Algorithm (must be RS256, not "none")

**For Local tokens**:

- ✅ Signature (using your configured secret key)
- ✅ Issuer (must match your config)
- ✅ Audience (must match your config)
- ✅ Expiry (token not expired)
- ✅ Algorithm (HS256 or RS256, not "none")

---

### Q: Where does token validation happen?

**A**: **Inside the client's backend application**, in-process. The Primus SDK is a library that runs as part of the client's API server. Validation logic executes locally; no network calls to Primus.

For Azure AD, the SDK fetches Microsoft's public keys (JWKS) and caches them. This is standard OAuth/OIDC behavior.

---

### Q: What if we don't use the SDK and implement validation ourselves?

**A**: You can, but it's **not recommended** because:

1. **Complexity**: Secure JWT validation requires 100+ lines of security-critical code
2. **Error-Prone**: Easy to miss edge cases (algorithm confusion, key rotation, etc.)
3. **Maintenance**: You're responsible for fixing security bugs
4. **Inconsistency**: Each client might implement differently
5. **No Support**: Primus can't help troubleshoot custom implementations

The SDK provides battle-tested validation logic while maintaining complete data isolation (runs in-process, no external calls).

---

## SDK vs Code Snippets

### Q: Why ship an SDK instead of just providing code snippets?

**A**: Several reasons:

1. **Security**: Validation logic is security-critical. SDK ensures correctness.
2. **Consistency**: All clients use the same tested implementation.
3. **Updates**: Bug fixes distributed via package updates (standard tooling).
4. **Developer Experience**: Easier integration = faster adoption.
5. **Support**: Clear versioning and issue tracking.

Code snippets would be:

- Hard to keep updated across all clients
- Prone to copy-paste errors
- Difficult to support (code diverges)
- No meaningful versioning

---

### Q: Does using the SDK mean we're dependent on Primus at runtime?

**A**: **No**. The SDK is a library, not a SaaS API:

- Runs in-process in your backend
- Makes no network calls to Primus servers
- Validates tokens locally
- Self-contained (includes all validation logic)

It's similar to using any other npm/NuGet package (like Express, Lodash, Newtonsoft.Json). The code runs in your infrastructure.

---

### Q: Can we see the SDK source code?

**A**: **Yes** (depending on your licensing model):

- Source code can be published on GitHub
- Clients can audit the code
- Transparent validation logic
- Can fork if customizations needed

Most clients will use the pre-built package, but transparency is available.

---

### Q: What if we need to customize the SDK behavior?

**A**: Options:

1. **Configuration**: SDK exposes many options (token lifetime, claim mappings, etc.)
2. **Events/Hooks**: SDK can emit events for custom logic (e.g., post-validation claims transformation)
3. **Fork**: Clients can fork the SDK repository and maintain their own version
4. **Feature Request**: Request features from Primus team

For most use cases, configuration is sufficient.

---

## Integration & Setup

### Q: How long does it take to integrate a Primus module?

**A**: For a typical .NET or Node API:

- **Basic setup**: 15-30 minutes
  - Install package
  - Add configuration
  - Wire up middleware

- **Full integration with testing**: 2-4 hours
  - Configure auth providers (Azure AD or local)
  - Implement login endpoints (for local mode)
  - Add authorization to routes
  - Test authentication flows

- **Production-ready**: 1-2 days
  - Comprehensive testing
  - Security review
  - Documentation for your team

---

### Q: What if our application uses a different stack (Python, Go, Java)?

**A**: v1 supports .NET and Node/TypeScript only. For other stacks:

- **Short-term**: Clients can implement validation using standard JWT libraries
- **Long-term**: Primus can add SDK support for popular stacks based on demand

The validation logic is standard OAuth/OIDC, so any language with JWT support can implement it.

---

### Q: Can we use Primus modules with existing authentication?

**A**: Yes, in **Hybrid mode**:

- Configure SDK with both Azure AD and Local settings
- Some routes can use Azure AD tokens
- Other routes can use local JWTs
- SDK validates both token types

Useful for:

- Migration scenarios (moving from local to Azure AD)
- Mixed user bases (employees use AD, customers use local)

---

### Q: Do we need to change our database schema?

**A**: **No**. Primus SDK doesn't require any specific database structure. You:

- Store users in your own tables with your own schema
- Use your own user IDs, usernames, roles
- Implement your own user management logic

The SDK only validates tokens; it doesn't manage user data.

---

## Azure AD Specifics

### Q: Do we need to register our app in Azure AD?

**A**: **Yes**, if using Azure AD mode. You need to:

1. Register an application in Azure AD (Entra ID)
2. Configure API permissions (if needed)
3. Note the Tenant ID, Client ID, and Audience
4. Configure these values in Primus SDK settings

This is standard Azure AD integration, unrelated to Primus. The SDK uses these values to validate tokens.

---

### Q: Does Primus have access to our Azure AD?

**A**: **No**. Primus never interacts with your Azure AD. The flow is:

1. Your frontend authenticates users with YOUR Azure AD
2. Your Azure AD issues tokens
3. Your backend validates tokens using Azure AD's public keys (JWKS)

Primus SDK facilitates #3 by implementing standard OIDC validation. It fetches public keys from Microsoft's well-known JWKS endpoint (publicly accessible).

---

### Q: What if we use multiple Azure AD tenants?

**A**: Configure SDK with the appropriate tenant ID for each environment:

- Dev environment → Dev tenant
- Prod environment → Prod tenant

Or implement multi-tenancy logic:

- Accept tokens from multiple tenant IDs
- Validate each token against the correct tenant's keys
- Determine tenant from token claims

SDK supports this through configuration.

---

## Local Authentication Specifics

### Q: How does local authentication work?

**A**: In Local mode:

1. **Your code** handles login:
   - Accept username/password from frontend
   - Validate credentials against your database
   - Use SDK helper to generate JWT token

2. **SDK handles validation**:
   - On subsequent requests, SDK validates JWT
   - Extracts user claims
   - Passes authenticated user context to your app

You control:

- User database schema
- Password hashing algorithm
- Login logic
- Token claims (user ID, roles, etc.)

SDK provides:

- JWT generation helper
- JWT validation middleware

---

### Q: Where are passwords stored?

**A**: **In your database**, not Primus. You:

- Create a Users table (your schema)
- Hash passwords (use bcrypt, Argon2, etc.)
- Validate credentials in your login endpoint

SDK doesn't touch passwords. It only generates/validates JWTs after you've verified credentials.

---

### Q: Can we use Primus for user management (registration, password reset)?

**A**: **No, not in v1**. Primus Identity Validator module only handles:

- Token validation
- JWT generation (helpers)

You implement:

- User registration
- Password reset
- Email verification
- Account management

These are application-specific and stay in your codebase.

---

## Compliance & Privacy

### Q: Is Primus GDPR compliant?

**A**: For end-user data, Primus is **not a data processor** because:

- No end-user PII stored by Primus
- All user data stays in client infrastructure
- Primus doesn't process personal data at runtime

For application metadata (admin email, app names), Primus can be GDPR compliant (standard SaaS compliance).

Clients remain the **data controllers** for their end users.

---

### Q: Do we need a Data Processing Agreement (DPA) with Primus?

**A**: Typically **no**, because Primus doesn't process end-user data. However:

- If admins provide PII in application names/descriptions, a DPA might apply
- Check with your legal team based on your specific setup

---

### Q: Can we meet data residency requirements?

**A**: **Yes**. Since the SDK runs in your infrastructure:

- Deploy backend in any region (EU, US, Asia, on-prem)
- Store user data in compliant locations
- Control data flow entirely

Primus Portal (metadata only) location doesn't affect end-user data residency.

---

## Troubleshooting

### Q: Authentication isn't working. How do we debug?

**A**: Check these common issues:

**Azure AD mode**:

1. Verify Azure AD config (Tenant ID, Client ID, Audience)
2. Check token is being sent in `Authorization: Bearer <token>` header
3. Verify frontend is getting token from correct Azure AD tenant
4. Check token expiry (token might be expired)
5. Review backend logs for validation errors

**Local mode**:

1. Verify signing key matches between token generation and validation
2. Check issuer/audience config matches
3. Verify token format (must be valid JWT)
4. Check token expiry
5. Ensure secret key is sufficiently complex

Enable debug logging in SDK for detailed error messages.

---

### Q: Token validation is slow. How do we optimize?

**A**: The SDK includes optimizations:

1. **JWKS Caching**: Azure AD public keys are cached (configurable TTL)
2. **In-Memory Validation**: No database or network calls for validation
3. **Connection Pooling**: Reuse HTTP connections for JWKS fetching

Additional tips:

- Use Redis for distributed JWKS cache (multi-instance backends)
- Increase JWKS cache TTL (balance between performance and key rotation)
- Monitor Azure AD JWKS endpoint latency

Typical validation latency: <1ms (cached), <50ms (cache miss).

---

### Q: We're getting "Invalid signature" errors. Why?

**A**: Common causes:

**Azure AD**:

- Token issued by different tenant than configured
- Clock skew between servers (sync server clocks)
- Token modified in transit (check for proxies/middleware tampering)
- Using wrong client ID in config

**Local**:

- Signing key mismatch (different key for generation vs validation)
- Wrong algorithm (HS256 vs RS256)
- Key rotation without updating config

Enable SDK debug logs to see exact validation errors.

---

## Pricing & Licensing

### Q: How is Primus SaaS Platform priced?

**A**: (This is a placeholder; define your pricing model)

Options:

1. **Per Application**: $X/month per registered application
2. **Per Module**: $Y/month per module per application
3. **Enterprise License**: Flat fee for unlimited applications
4. **Open Source**: Free SDK, paid support/portal access

---

### Q: Can we use Primus SDK for free?

**A**: Depends on your licensing model. Options:

1. SDK is open source (MIT license), portal is paid
2. SDK is free for limited applications, paid for scale
3. SDK + Portal bundled pricing

Clarify with your product team.

---

## Roadmap & Future Features

### Q: What modules are planned after Identity Validator?

**A**: Potential future modules:

- **RBAC (Role-Based Access Control)**: Fine-grained permission system
- **Logging**: Structured logging with correlation IDs
- **Rate Limiting**: API rate limiting and throttling
- **Audit Trail**: User activity logging
- **Notifications**: Email/SMS notifications
- **Feature Flags**: Toggle features per client/user

Priority based on client demand.

---

### Q: Will client developers get portal access in future versions?

**A**: Potentially in v2+. Possible features:

- Client self-service application registration
- View Documentation in portal
- Track module usage and updates
- Request new modules or features

For v1, admin-only portal keeps complexity low.

---

### Q: Will there be SDKs for other languages (Python, Go, Java)?

**A**: Based on demand. The validation logic is standard OAuth/OIDC, so:

- **Short-term**: Clients can use standard JWT libraries
- **Long-term**: Primus can publish official SDKs for popular stacks

Vote on GitHub issues or contact Primus team to prioritize.

---

## Contact & Support

### Q: How do we get support?

**A**: Support channels:

1. **GitHub Issues**: For SDK bugs or feature requests
2. **Email**: admin@primus.com for portal/account issues
3. **Documentation**: Comprehensive docs in `/docs` folder
4. **Slack/Discord**: (If you set up a community channel)

---

### Q: Can we contribute to the SDK?

**A**: If SDK is open source:

- Yes! Submit PRs on GitHub
- Follow contribution guidelines
- Add tests for new features

If proprietary:

- Submit feature requests via GitHub issues
- Primus team will evaluate and implement

---

**Have more questions?** Contact the Primus SaaS Platform team or check the [Architecture Documentation](./ARCHITECTURE.md).

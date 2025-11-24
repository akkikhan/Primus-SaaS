# Identity Validator Configuration

This guide covers all configuration options for the Primus Identity Validator SDKs (.NET and Node.js).

## .NET Configuration

Configure the service in `Program.cs`:

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    // List of trusted token issuers
    options.Issuers = new List<IssuerConfig>
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.Oidc,
            Authority = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
            Audiences = new List<string> { "api://your-api-id" }
        },
        new IssuerConfig
        {
            Name = "Auth0",
            Type = IssuerType.Oidc,
            Authority = "https://your-domain.auth0.com/",
            Audiences = new List<string> { "https://api.your-domain.com" }
        }
    };

    // global options
    options.ValidateLifetime = true;
    options.ClockSkew = TimeSpan.FromMinutes(5);
});
```

### Options Reference (.NET)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Issuers` | `List<IssuerConfig>` | `[]` | List of configured token issuers. |
| `ValidateLifetime` | `bool` | `true` | Whether to validate token expiration. |
| `RequireHttpsMetadata` | `bool` | `true` | Require HTTPS for OIDC discovery (disable for local dev). |
| `ClockSkew` | `TimeSpan` | `5 min` | Allowed clock skew for time validation. |

---

## Node.js Configuration

Configure the validator when initializing the class:

```javascript
const validator = new PrimusIdentityValidator({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/<TENANT_ID>/v2.0',
      authority: 'https://login.microsoftonline.com/<TENANT_ID>/v2.0',
      audiences: ['api://your-api-id']
    }
  ],
  clockSkew: 300 // seconds
});
```

### Options Reference (Node.js)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `issuers` | `Array` | `[]` | List of issuer configurations. |
| `clockSkew` | `number` | `300` | Allowed clock skew in seconds. |
| `logLevel` | `string` | `'info'` | Logging level (debug, info, error). |

---

## Issuer Configuration

Both SDKs use a similar structure for defining issuers.

### OIDC Issuer (Azure AD, Auth0, Okta)

```json
{
  "name": "ProviderName",
  "type": "oidc",
  "authority": "https://provider.com/oauth/v2",
  "audiences": ["api://my-api"]
}
```

### JWT Issuer (Custom, Local)

```json
{
  "name": "LocalAuth",
  "type": "jwt",
  "issuer": "https://my-auth-server.com",
  "secret": "my-super-secret-key",
  "audiences": ["api://my-api"]
}
```

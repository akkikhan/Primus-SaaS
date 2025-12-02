# Identity Advanced Example

Multi-issuer setup (Azure AD + Local JWT), Swagger security, and diagnostics.

## Run
```bash
dotnet restore
dotnet run
```

## Test
- Swagger UI: `http://localhost:5000/swagger`
- Diagnostics: `GET http://localhost:5000/primus/diagnostics` (dev only)
- Secure endpoint: `GET http://localhost:5000/secure` with Bearer token for configured issuer/audience.

## Config
- Update `PrimusIdentity.Issuers` for Azure AD (tenant/client) and LocalDev secret (32+ chars). Use User Secrets/Key Vault for secrets.

# Identity Minimal Example

Minimal API showing Primus Identity with local JWT validation and Swagger.

## Run
```bash
# Restore the project (avoids MSB1011 if run outside this folder)
dotnet restore IdentityMinimal.csproj

# Run the app (bind to a URL for easy testing)
dotnet run --project IdentityMinimal.csproj --urls=http://localhost:5000
```

## Test
- GET `http://localhost:5000/public` (no auth)
- GET `http://localhost:5000/secure` (requires Bearer token for issuer `https://localhost:5001`, audience `api://local-dev`)
- Swagger UI (dev only): `http://localhost:5000/swagger`

## Config
- Update `appsettings.json` `PrimusIdentity:Issuers[0]:Secret` with a 32+ char secret (store in User Secrets/Key Vault for real use).

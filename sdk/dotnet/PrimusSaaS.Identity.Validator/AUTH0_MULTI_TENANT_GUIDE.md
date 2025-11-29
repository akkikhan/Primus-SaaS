# Auth0 Multi-Tenant Guidance (Primus Identity Validator)

Goal: make it explicit how to register multiple Auth0 domains/issuers without guessing.

## Quick config (multiple tenants)
```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new()
    {
        new()
        {
            Name = "Auth0-EU",
            Type = IssuerType.Auth0,
            Issuer = "https://your-eu-tenant.eu.auth0.com/",
            Authority = "https://your-eu-tenant.eu.auth0.com/",
            Audiences = { "api://your-api" },
            AllowMachineToMachine = true,
            AllowedGrantTypes = { "client_credentials" }
        },
        new()
        {
            Name = "Auth0-US",
            Type = IssuerType.Auth0,
            Issuer = "https://your-us-tenant.us.auth0.com/",
            Authority = "https://your-us-tenant.us.auth0.com/",
            Audiences = { "api://your-api" },
            AllowMachineToMachine = true,
            AllowedGrantTypes = { "client_credentials" }
        }
    };
});
```

## Management API client bundling
When you need Management API calls, pull `Auth0.ManagementApi` alongside `Auth0.AspNetCore.Authentication`:
```bash
dotnet add package Auth0.ManagementApi
dotnet add package Auth0.AspNetCore.Authentication
```
Keep the Management client scoped:
```csharp
builder.Services.AddSingleton(sp =>
{
    var domain = "your-tenant.auth0.com";
    var clientId = builder.Configuration["Auth0:Mgmt:ClientId"];
    var clientSecret = builder.Configuration["Auth0:Mgmt:ClientSecret"];
    var managementToken = new ManagementApiTokenClient(domain, clientId, clientSecret);
    var token = managementToken.GetTokenAsync().GetAwaiter().GetResult();
    return new ManagementApiClient(token.AccessToken, $"https://{domain}/api/v2");
});
```

## Tips
- Issuer must match token `iss` exactly (with trailing slash).
- Audience must match your API identifier.
- Use different `Name` per issuer to surface via `primus:issuer_name` claim.
- Enable `AllowMachineToMachine` and `AllowedGrantTypes` for client_credentials flows.***

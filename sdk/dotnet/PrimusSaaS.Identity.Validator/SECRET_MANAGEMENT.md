# Secret Management Guide - .NET SDK

> [!CAUTION]
> **Never commit secrets to source control!** Use Azure Key Vault, environment variables, or other secure secret management solutions.

## Table of Contents

1. [Azure Key Vault (Production)](#azure-key-vault-production)
2. [User Secrets (Development)](#user-secrets-development)
3. [Environment Variables](#environment-variables)
4. [CI/CD Pipeline Secrets](#cicd-pipeline-secrets)

---

## Azure Key Vault (Production)

For production deployments on Azure, **Key Vault** is the recommended way to manage secrets.

### 1. Install Packages

```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

### 2. Configure Program.cs

```csharp
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVaultUrl"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential()
    );
}

// Secrets are now transparently available in Configuration
builder.Services.AddPrimusIdentity(options =>
{
    // This binds values from Key Vault automatically
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});
```

### 3. Set Secrets in Key Vault

Use double dashes `--` to represent nested JSON hierarchy.

| JSON Path | Key Vault Name |
|-----------|----------------|
| `PrimusIdentity:Issuers:0:Secret` | `PrimusIdentity--Issuers--0--Secret` |
| `PrimusIdentity:Issuers:1:Secret` | `PrimusIdentity--Issuers--1--Secret` |

```bash
# Azure CLI Example
az keyvault secret set \
    --vault-name "my-keyvault" \
    --name "PrimusIdentity--Issuers--0--Secret" \
    --value "your-super-secure-production-secret"
```

---

## User Secrets (Development)

For local development, use the **Secret Manager** tool to keep secrets out of your code.

### 1. Initialize Secret Manager

Run this in your project directory:

```bash
dotnet user-secrets init
```

### 2. Set Secrets

```bash
dotnet user-secrets set "PrimusIdentity:Issuers:0:Secret" "dev-secret-key-123"
```

### 3. Access in Code

User secrets are automatically loaded in `Development` environment by default. No extra code needed!

```csharp
// In Program.cs
var secret = builder.Configuration["PrimusIdentity:Issuers:0:Secret"];
```

---

## Environment Variables

For containerized deployments (Docker, Kubernetes) or non-Azure hosting.

### Format

Use double underscores `__` for nesting.

- `PrimusIdentity__Issuers__0__Secret`
- `PrimusIdentity__Issuers__0__Issuer`

### Docker Example

```yaml
# docker-compose.yml
services:
  api:
    image: my-api
    environment:
      - PrimusIdentity__Issuers__0__Secret=production-secret-key
      - PrimusIdentity__Issuers__0__Issuer=https://auth.prod.com
```

---

## CI/CD Pipeline Secrets

### GitHub Actions

1. Go to **Settings > Secrets and variables > Actions**.
2. Add repository secret: `PRIMUS_IDENTITY_SECRET`.
3. Inject into build/test step:

```yaml
- name: Run Tests
  run: dotnet test
  env:
    PrimusIdentity__Issuers__0__Secret: ${{ secrets.PRIMUS_IDENTITY_SECRET }}
```

### Azure DevOps

1. Go to **Library > Variable Groups**.
2. Add variable: `PrimusIdentity.Issuers.0.Secret`.
3. Link variable group to pipeline.

---

## Best Practices Checklist

- [ ] **Development**: Use User Secrets (`dotnet user-secrets`).
- [ ] **Production**: Use Azure Key Vault or Environment Variables.
- [ ] **Repo**: Add `appsettings.json` (with empty/dummy secrets) to git.
- [ ] **Repo**: Add `appsettings.Development.json` (with non-sensitive dev defaults) to git.
- [ ] **Ignored**: Ensure `appsettings.Production.json` (if used for secrets) is in `.gitignore`.

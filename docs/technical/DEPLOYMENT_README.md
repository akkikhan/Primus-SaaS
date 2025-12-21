# Deployment Configuration - README

This directory contains all necessary files for deploying Primus SaaS to Microsoft Azure.

## Files Overview

### Deployment Scripts

#### `deploy-azure.ps1`
PowerShell script for automated Azure deployment using Azure CLI.

**Features:**
- Creates all Azure resources (Resource Group, SQL Database, App Service, Key Vault, etc.)
- Configures app settings and connection strings
- Sets up managed identity and Key Vault integration
- Configures CORS and security settings

**Usage:**
```powershell
# Development environment
.\deploy-azure.ps1 -Environment dev -Location eastus

# Production environment
.\deploy-azure.ps1 -Environment prod -ResourceGroup "primus-saas-prod-rg"

# Infrastructure only (no code deployment)
.\deploy-azure.ps1 -Environment dev -SkipInfrastructure
```

**Parameters:**
- `-ResourceGroup` - Azure resource group name (default: primus-saas-rg)
- `-Location` - Azure region (default: eastus)
- `-Environment` - Environment name: dev, staging, prod (default: dev)
- `-SqlAdminUsername` - SQL admin username (default: primusadmin)
- `-SkipInfrastructure` - Skip infrastructure creation
- `-DeployOnly` - Skip resource group creation

### GitHub Actions Workflows

Located in `.github/workflows/`:

#### `backend-deploy.yml`
Backend API CI/CD pipeline
- Builds .NET 7.0 application
- Runs tests with coverage
- Deploys to Azure App Service
- Triggers on push to master/dev-* branches

#### `frontend-deploy.yml`
Frontend CI/CD pipeline
- Builds React + Vite application
- Runs linter and tests
- Deploys to Azure Static Web Apps
- Triggers on push to master/dev-* branches

#### `sdk-dotnet.yml`
.NET SDK CI/CD pipeline
- Builds and tests PrimusSaaS.Identity.Validator
- Publishes to NuGet.org (on master)
- Runs on push to sdk/dotnet/**

#### `sdk-nodejs.yml`
Node.js SDK CI/CD pipeline
- Builds and tests primus-identity-validator
- Publishes to NPM (on master)
- Runs on push to sdk/nodejs/**

### Documentation

#### `docs/DEPLOYMENT.md`
Comprehensive deployment guide covering:
- Prerequisites and architecture overview
- Step-by-step deployment instructions
- Manual Azure CLI commands
- GitHub Actions setup
- Post-deployment configuration
- Monitoring and troubleshooting
- Common issues and solutions

#### `docs/AZURE_CLI_REFERENCE.md`
Quick reference for Azure CLI commands:
- Authentication and account management
- Resource group operations
- SQL Database management
- App Service commands
- Static Web Apps
- Key Vault operations
- Application Insights
- Monitoring and diagnostics
- Cost management

## Deployment Workflow

### 1. Initial Setup

```powershell
# Install Azure CLI
# https://aka.ms/azure-cli

# Login to Azure
az login

# Set subscription
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

### 2. Run Deployment Script

```powershell
# Navigate to repository root
cd "Primus SaaS"

# Run deployment
.\deploy-azure.ps1 -Environment dev
```

### 3. Configure GitHub Secrets

Add these secrets to GitHub repository:
- `AZURE_CREDENTIALS` - Service principal JSON
- `AZURE_STATIC_WEB_APPS_API_TOKEN` - Static Web App token
- `VITE_API_URL` - Backend API URL
- `NUGET_API_KEY` - (Optional) NuGet publishing
- `NPM_TOKEN` - (Optional) NPM publishing

### 4. Trigger Deployment

```bash
git push origin dev-9
```

GitHub Actions will automatically:
- Build and test backend
- Build and test frontend
- Deploy to Azure environments
- Run database migrations (if configured)

## Azure Resources Created

The deployment creates:

| Resource | Type | Purpose |
|----------|------|---------|
| primus-saas-rg | Resource Group | Container for all resources |
| primus-saas-dev-sql | SQL Server | Database server |
| PrimusSaasDb | SQL Database | Application database |
| primus-saas-dev-api | App Service | Backend API hosting |
| primus-saas-dev-web | Static Web App | Frontend hosting |
| primus-saas-dev-kv-* | Key Vault | Secrets management |
| primus-saas-dev-ai | Application Insights | Monitoring and logging |
| primus-saas-dev-plan | App Service Plan | Hosting plan |

## Environment Configuration

### Development
- Resource Group: `primus-saas-dev-rg`
- SKU: B1 (Basic)
- SQL Tier: S0 (Standard)
- Location: East US

### Staging
- Resource Group: `primus-saas-staging-rg`
- SKU: S1 (Standard)
- SQL Tier: S1 (Standard)
- Location: East US

### Production
- Resource Group: `primus-saas-prod-rg`
- SKU: P1V2 (Premium)
- SQL Tier: S2 (Standard)
- Location: East US

## Post-Deployment Steps

1. **Update Frontend Configuration**
   ```bash
   # Create .env.production
   echo "VITE_API_URL=https://primus-saas-dev-api.azurewebsites.net" > portal/frontend/.env.production
   ```

2. **Run Database Migrations**
   ```bash
   cd portal/backend
   dotnet ef database update
   ```

3. **Verify Deployment**
   - Backend: https://primus-saas-dev-api.azurewebsites.net/api/weatherforecast
   - Frontend: https://primus-saas-dev-web.azurestaticapps.net
   - Portal: https://portal.azure.com

4. **Configure Custom Domains** (Optional)
   ```bash
   az staticwebapp hostname set --name primus-saas-dev-web --hostname app.example.com
   az webapp config hostname add --webapp-name primus-saas-dev-api --hostname api.example.com
   ```

## Monitoring

### Application Insights
Access metrics and logs:
```bash
az monitor app-insights component show --app primus-saas-dev-ai --resource-group primus-saas-dev-rg
```

### Stream Logs
```bash
az webapp log tail --name primus-saas-dev-api --resource-group primus-saas-dev-rg
```

### View in Azure Portal
Navigate to:
- Resource Group: https://portal.azure.com/#@/resource/subscriptions/{subscription-id}/resourceGroups/primus-saas-dev-rg
- Application Insights: Dashboard → Metrics → Live Metrics

## Cost Estimation

**Development Environment (Monthly):**
- App Service (B1): ~$13
- SQL Database (S0): ~$15
- Static Web App (Free tier): $0
- Key Vault (Standard): $0.03 + operations
- Application Insights (First 5GB): $0
- **Total**: ~$30-35/month

**Production Environment (Monthly):**
- App Service (P1V2): ~$125
- SQL Database (S2): ~$60
- Static Web App (Standard): ~$9
- Key Vault (Standard): $0.03 + operations
- Application Insights: Based on usage
- **Total**: ~$200-250/month

## Troubleshooting

### Common Issues

1. **Deployment Script Fails**
   - Verify Azure CLI is installed: `az --version`
   - Check login status: `az account show`
   - Ensure sufficient permissions

2. **Resource Already Exists**
   - Use different resource names
   - Delete existing resources: `az group delete --name primus-saas-dev-rg`

3. **GitHub Actions Fails**
   - Verify secrets are configured
   - Check workflow logs in Actions tab
   - Ensure service principal has correct permissions

4. **Database Connection Issues**
   - Verify firewall rules
   - Check connection string in app settings
   - Ensure managed identity has Key Vault access

### Getting Help

- Check `docs/DEPLOYMENT.md` for detailed troubleshooting
- Review Azure Portal activity logs
- Check Application Insights for runtime errors
- Consult `docs/AZURE_CLI_REFERENCE.md` for CLI commands

## Security Best Practices

1. **Secrets Management**
   - All secrets stored in Key Vault
   - Managed Identity for authentication
   - No hardcoded credentials

2. **Network Security**
   - CORS configured for allowed origins
   - SQL Server firewall rules
   - HTTPS enforced

3. **Access Control**
   - Service principal with minimal permissions
   - RBAC roles properly assigned
   - Regular access reviews

4. **Monitoring**
   - Application Insights enabled
   - Alert rules configured
   - Regular security audits

## Maintenance

### Regular Tasks

- **Weekly**: Review Application Insights logs
- **Monthly**: Check cost analysis and optimize resources
- **Quarterly**: Update dependencies and security patches
- **Yearly**: Review and rotate secrets

### Backup Strategy

- **Database**: Automated backups (30-day retention)
- **App Service**: Configuration backups
- **Key Vault**: Soft-delete enabled (90-day recovery)

## Support

For issues or questions:
1. Check documentation in `docs/` directory
2. Review GitHub Actions logs
3. Check Azure Portal diagnostics
4. Contact Azure support if needed

## Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure Static Web Apps Documentation](https://docs.microsoft.com/en-us/azure/static-web-apps/)
- [Azure SQL Database Documentation](https://docs.microsoft.com/en-us/azure/azure-sql/)
- [GitHub Actions for Azure](https://docs.microsoft.com/en-us/azure/developer/github/github-actions)
- [Azure CLI Reference](https://docs.microsoft.com/en-us/cli/azure/reference-index)

# Primus SaaS - Azure Deployment Guide

This guide provides step-by-step instructions for deploying the Primus SaaS platform to Microsoft Azure.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Architecture Overview](#architecture-overview)
- [Deployment Options](#deployment-options)
- [Quick Start - Automated Deployment](#quick-start---automated-deployment)
- [Manual Deployment Steps](#manual-deployment-steps)
- [GitHub Actions CI/CD Setup](#github-actions-cicd-setup)
- [Post-Deployment Configuration](#post-deployment-configuration)
- [Monitoring and Troubleshooting](#monitoring-and-troubleshooting)

## Prerequisites

### Required Tools

1. **Azure CLI** - [Install Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
   ```bash
   # Verify installation
   az --version
   ```

2. **Azure Subscription** - Active Azure subscription with appropriate permissions
   - Contributor or Owner role at subscription or resource group level

3. **GitHub Account** - For CI/CD automation (optional but recommended)

4. **.NET 7.0 SDK** - For backend deployment
   ```bash
   dotnet --version
   ```

5. **Node.js 18+** - For frontend build
   ```bash
   node --version
   npm --version
   ```

### Azure Resources to be Created

- **Resource Group** - Container for all resources
- **Azure SQL Database** - Production database (replaces SQLite)
- **App Service Plan** - Hosting plan (B1 SKU)
- **App Service** - Backend API hosting
- **Static Web App** - Frontend hosting
- **Key Vault** - Secrets management
- **Application Insights** - Monitoring and logging

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      Azure Cloud                            │
│                                                              │
│  ┌──────────────────┐         ┌──────────────────┐         │
│  │  Static Web App  │         │   App Service    │         │
│  │   (Frontend)     │────────▶│   (Backend API)  │         │
│  │   React + Vite   │  HTTPS  │   ASP.NET Core   │         │
│  └──────────────────┘         └──────────────────┘         │
│          │                            │                     │
│          │                            ▼                     │
│          │                    ┌──────────────────┐         │
│          │                    │   Azure SQL DB   │         │
│          │                    │                  │         │
│          │                    └──────────────────┘         │
│          │                            │                     │
│          │                            ▼                     │
│          │                    ┌──────────────────┐         │
│          └───────────────────▶│    Key Vault     │         │
│                                │  (Secrets)       │         │
│                                └──────────────────┘         │
│                                        │                     │
│                                        ▼                     │
│                                ┌──────────────────┐         │
│                                │ App Insights     │         │
│                                │ (Monitoring)     │         │
│                                └──────────────────┘         │
└─────────────────────────────────────────────────────────────┘
```

## Deployment Options

### Option 1: Automated Script (Recommended)
Use the PowerShell deployment script for quick setup.

### Option 2: Manual Azure CLI Commands
Step-by-step Azure CLI commands for full control.

### Option 3: Azure Portal
Manual creation through Azure Portal (not covered in detail).

## Quick Start - Automated Deployment

### 1. Login to Azure

```powershell
az login
```

### 2. Set Active Subscription (if you have multiple)

```powershell
az account list --output table
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

### 3. Run Deployment Script

```powershell
# Deploy to development environment
.\deploy-azure.ps1 -Environment dev -Location eastus

# Deploy to production environment
.\deploy-azure.ps1 -Environment prod -Location eastus -ResourceGroup "primus-saas-prod-rg"

# Deploy infrastructure only (skip deployment)
.\deploy-azure.ps1 -Environment dev -SkipInfrastructure
```

### 4. Note the Output

The script will display important information:
- Resource URLs
- Database connection details
- Key Vault name
- Next steps for code deployment

## Manual Deployment Steps

If you prefer manual control, follow these steps:

### Step 1: Create Resource Group

```bash
az group create \
  --name primus-saas-rg \
  --location eastus
```

### Step 2: Create Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --name primus-saas-sql \
  --resource-group primus-saas-rg \
  --location eastus \
  --admin-user primusadmin \
  --admin-password 'YourSecurePassword123!'

# Configure firewall
az sql server firewall-rule create \
  --resource-group primus-saas-rg \
  --server primus-saas-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Create database
az sql db create \
  --resource-group primus-saas-rg \
  --server primus-saas-sql \
  --name PrimusSaasDb \
  --service-objective S0
```

### Step 3: Create Key Vault

```bash
az keyvault create \
  --name primus-saas-kv-123 \
  --resource-group primus-saas-rg \
  --location eastus

# Store secrets
az keyvault secret set \
  --vault-name primus-saas-kv-123 \
  --name SqlAdminPassword \
  --value 'YourSecurePassword123!'

az keyvault secret set \
  --vault-name primus-saas-kv-123 \
  --name JwtSecret \
  --value 'YourRandomJwtSecret64CharactersLong'
```

### Step 4: Create Application Insights

```bash
az monitor app-insights component create \
  --app primus-saas-ai \
  --location eastus \
  --resource-group primus-saas-rg \
  --application-type web
```

### Step 5: Create App Service Plan

```bash
az appservice plan create \
  --name primus-saas-plan \
  --resource-group primus-saas-rg \
  --location eastus \
  --sku B1 \
  --is-linux
```

### Step 6: Create and Configure Backend App Service

```bash
# Create app service
az webapp create \
  --name primus-saas-api \
  --resource-group primus-saas-rg \
  --plan primus-saas-plan \
  --runtime "DOTNET|7.0"

# Configure app settings
az webapp config appsettings set \
  --name primus-saas-api \
  --resource-group primus-saas-rg \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Server=tcp:primus-saas-sql.database.windows.net,1433;Initial Catalog=PrimusSaasDb;..." \
    JwtSettings__Secret=@Microsoft.KeyVault(SecretUri=https://primus-saas-kv-123.vault.azure.net/secrets/JwtSecret/)

# Enable managed identity
az webapp identity assign \
  --name primus-saas-api \
  --resource-group primus-saas-rg

# Grant Key Vault access
PRINCIPAL_ID=$(az webapp identity show --name primus-saas-api --resource-group primus-saas-rg --query principalId -o tsv)

az keyvault set-policy \
  --name primus-saas-kv-123 \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Configure CORS
az webapp cors add \
  --name primus-saas-api \
  --resource-group primus-saas-rg \
  --allowed-origins https://primus-saas-web.azurestaticapps.net http://localhost:5173
```

### Step 7: Deploy Backend Code

```bash
cd portal/backend

# Build and publish
dotnet publish -c Release -o ./publish

# Create zip package
cd publish
zip -r ../backend.zip .
cd ..

# Deploy to Azure
az webapp deploy \
  --resource-group primus-saas-rg \
  --name primus-saas-api \
  --src-path backend.zip \
  --type zip
```

### Step 8: Run Database Migrations

```bash
# Update connection string in appsettings.json temporarily
# Then run migrations
dotnet ef database update
```

### Step 9: Create Static Web App

```bash
# Note: Static Web Apps require GitHub integration
# Use Azure Portal or provide GitHub token

az staticwebapp create \
  --name primus-saas-web \
  --resource-group primus-saas-rg \
  --location eastus \
  --source https://github.com/akkikhan/Primus-SaaS \
  --branch dev-9 \
  --app-location "/portal/frontend" \
  --output-location "dist" \
  --token YOUR_GITHUB_TOKEN
```

## GitHub Actions CI/CD Setup

### Prerequisites

1. **Azure Service Principal** for authentication
2. **GitHub Secrets** configured
3. **Static Web App deployment token**

### Step 1: Create Azure Service Principal

```bash
# Create service principal
az ad sp create-for-rbac \
  --name "primus-saas-github-actions" \
  --role contributor \
  --scopes /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/primus-saas-rg \
  --sdk-auth

# Copy the entire JSON output
```

### Step 2: Configure GitHub Secrets

Add these secrets to your GitHub repository (Settings → Secrets → Actions):

1. **AZURE_CREDENTIALS** - The JSON output from service principal creation
2. **AZURE_STATIC_WEB_APPS_API_TOKEN** - Get from Static Web App in Azure Portal
3. **VITE_API_URL** - Your backend API URL (https://primus-saas-api.azurewebsites.net)
4. **NUGET_API_KEY** - (Optional) For publishing .NET SDK to NuGet
5. **NPM_TOKEN** - (Optional) For publishing Node.js SDK to NPM

### Step 3: Verify Workflows

The following workflows are configured:

- **backend-deploy.yml** - Builds and deploys backend API
- **frontend-deploy.yml** - Builds and deploys frontend
- **sdk-dotnet.yml** - Builds, tests, and publishes .NET SDK
- **sdk-nodejs.yml** - Builds, tests, and publishes Node.js SDK

### Step 4: Trigger Deployment

```bash
# Push to trigger deployment
git add .
git commit -m "Configure Azure deployment"
git push origin dev-9
```

## Post-Deployment Configuration

### 1. Update Frontend Environment Variables

Create `.env.production` in `portal/frontend`:

```env
VITE_API_URL=https://primus-saas-api.azurewebsites.net
```

### 2. Configure Custom Domains (Optional)

```bash
# Add custom domain to Static Web App
az staticwebapp hostname set \
  --name primus-saas-web \
  --resource-group primus-saas-rg \
  --hostname app.yourcompany.com

# Add custom domain to App Service
az webapp config hostname add \
  --webapp-name primus-saas-api \
  --resource-group primus-saas-rg \
  --hostname api.yourcompany.com
```

### 3. Enable SSL/TLS

```bash
# Managed certificate for custom domain
az webapp config ssl create \
  --resource-group primus-saas-rg \
  --name primus-saas-api \
  --hostname api.yourcompany.com
```

### 4. Configure Database Firewall

```bash
# Add your IP for database management
az sql server firewall-rule create \
  --resource-group primus-saas-rg \
  --server primus-saas-sql \
  --name MyWorkstation \
  --start-ip-address YOUR_IP \
  --end-ip-address YOUR_IP
```

### 5. Seed Initial Data

```bash
# Access backend app service via SSH or use Azure CLI
# Run seed commands or access admin portal
```

## Monitoring and Troubleshooting

### View Application Logs

```bash
# Stream backend logs
az webapp log tail \
  --name primus-saas-api \
  --resource-group primus-saas-rg

# Download logs
az webapp log download \
  --name primus-saas-api \
  --resource-group primus-saas-rg \
  --log-file logs.zip
```

### Application Insights

Access Application Insights in Azure Portal:
- Navigate to: `primus-saas-ai` resource
- View: Live Metrics, Failures, Performance, Availability

### Common Issues

#### 1. Database Connection Fails

**Symptom**: Backend returns 500 errors on startup

**Solution**:
```bash
# Verify firewall rules
az sql server firewall-rule list \
  --resource-group primus-saas-rg \
  --server primus-saas-sql

# Check connection string in app settings
az webapp config appsettings list \
  --name primus-saas-api \
  --resource-group primus-saas-rg
```

#### 2. CORS Errors

**Symptom**: Frontend can't call backend API

**Solution**:
```bash
# Check CORS settings
az webapp cors show \
  --name primus-saas-api \
  --resource-group primus-saas-rg

# Add frontend URL
az webapp cors add \
  --name primus-saas-api \
  --resource-group primus-saas-rg \
  --allowed-origins https://your-frontend-url.azurestaticapps.net
```

#### 3. Key Vault Access Denied

**Symptom**: Backend can't read secrets from Key Vault

**Solution**:
```bash
# Verify managed identity is enabled
az webapp identity show \
  --name primus-saas-api \
  --resource-group primus-saas-rg

# Grant access policy
PRINCIPAL_ID=$(az webapp identity show --name primus-saas-api --resource-group primus-saas-rg --query principalId -o tsv)

az keyvault set-policy \
  --name primus-saas-kv-123 \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

#### 4. Static Web App Build Fails

**Symptom**: GitHub Actions fails to deploy frontend

**Solution**:
- Check build logs in GitHub Actions
- Verify `app-location` and `output-location` in workflow
- Ensure environment variables are set in GitHub Secrets

### Health Checks

```bash
# Check backend health
curl https://primus-saas-api.azurewebsites.net/api/weatherforecast

# Check database connectivity
az sql db show \
  --resource-group primus-saas-rg \
  --server primus-saas-sql \
  --name PrimusSaasDb
```

## Resource Management

### Scaling

```bash
# Scale App Service Plan
az appservice plan update \
  --name primus-saas-plan \
  --resource-group primus-saas-rg \
  --sku S1

# Enable autoscaling (requires Standard tier or higher)
az monitor autoscale create \
  --resource-group primus-saas-rg \
  --resource primus-saas-plan \
  --resource-type Microsoft.Web/serverfarms \
  --name autoscale-plan \
  --min-count 1 \
  --max-count 3 \
  --count 1
```

### Backup

```bash
# Configure automatic backups (requires Standard tier)
az webapp config backup update \
  --resource-group primus-saas-rg \
  --webapp-name primus-saas-api \
  --container-url "STORAGE_SAS_URL" \
  --frequency 1d \
  --retain-one true \
  --retention 30

# Manual backup
az webapp config backup create \
  --resource-group primus-saas-rg \
  --webapp-name primus-saas-api \
  --container-url "STORAGE_SAS_URL" \
  --backup-name manual-backup
```

### Cost Management

```bash
# View costs by resource group
az consumption usage list \
  --start-date 2024-11-01 \
  --end-date 2024-11-30 \
  | jq '[.[] | select(.instanceLocation=="primus-saas-rg")] | group_by(.meterCategory) | map({category: .[0].meterCategory, cost: (map(.pretaxCost) | add)})'
```

## Cleanup

To delete all resources:

```bash
# Delete entire resource group (CAUTION!)
az group delete \
  --name primus-saas-rg \
  --yes \
  --no-wait
```

## Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure Static Web Apps Documentation](https://docs.microsoft.com/en-us/azure/static-web-apps/)
- [Azure SQL Database Documentation](https://docs.microsoft.com/en-us/azure/azure-sql/)
- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [GitHub Actions for Azure](https://docs.microsoft.com/en-us/azure/developer/github/github-actions)

## Support

For issues or questions:
1. Check Application Insights logs
2. Review GitHub Actions workflow logs
3. Consult Azure documentation
4. Contact Azure support if needed

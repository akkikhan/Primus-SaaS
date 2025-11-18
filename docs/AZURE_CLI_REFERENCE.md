# Azure CLI Quick Reference for Primus SaaS

Quick reference for common Azure CLI commands used in Primus SaaS deployment and management.

## Authentication

```bash
# Login to Azure
az login

# Login with service principal (CI/CD)
az login --service-principal -u APP_ID -p PASSWORD --tenant TENANT_ID

# Show current account
az account show

# List all subscriptions
az account list --output table

# Set active subscription
az account set --subscription "SUBSCRIPTION_ID"

# Logout
az logout
```

## Resource Groups

```bash
# Create resource group
az group create --name primus-saas-rg --location eastus

# List resource groups
az group list --output table

# Show resource group
az group show --name primus-saas-rg

# Delete resource group
az group delete --name primus-saas-rg --yes --no-wait

# Check if resource group exists
az group exists --name primus-saas-rg
```

## Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --name primus-saas-sql \
  --resource-group primus-saas-rg \
  --location eastus \
  --admin-user primusadmin \
  --admin-password 'SecurePassword123!'

# List SQL Servers
az sql server list --resource-group primus-saas-rg --output table

# Create database
az sql db create \
  --resource-group primus-saas-rg \
  --server primus-saas-sql \
  --name PrimusSaasDb \
  --service-objective S0

# List databases
az sql db list --resource-group primus-saas-rg --server primus-saas-sql --output table

# Add firewall rule
az sql server firewall-rule create \
  --resource-group primus-saas-rg \
  --server primus-saas-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# List firewall rules
az sql server firewall-rule list \
  --resource-group primus-saas-rg \
  --server primus-saas-sql \
  --output table

# Get connection string
az sql db show-connection-string \
  --client ado.net \
  --server primus-saas-sql \
  --name PrimusSaasDb
```

## App Service

```bash
# Create App Service Plan
az appservice plan create \
  --name primus-saas-plan \
  --resource-group primus-saas-rg \
  --location eastus \
  --sku B1 \
  --is-linux

# List App Service Plans
az appservice plan list --resource-group primus-saas-rg --output table

# Create Web App
az webapp create \
  --name primus-saas-api \
  --resource-group primus-saas-rg \
  --plan primus-saas-plan \
  --runtime "DOTNET|7.0"

# List Web Apps
az webapp list --resource-group primus-saas-rg --output table

# Start/Stop/Restart Web App
az webapp start --name primus-saas-api --resource-group primus-saas-rg
az webapp stop --name primus-saas-api --resource-group primus-saas-rg
az webapp restart --name primus-saas-api --resource-group primus-saas-rg

# Deploy code
az webapp deploy \
  --resource-group primus-saas-rg \
  --name primus-saas-api \
  --src-path ./backend.zip \
  --type zip

# Configure app settings
az webapp config appsettings set \
  --name primus-saas-api \
  --resource-group primus-saas-rg \
  --settings KEY1=VALUE1 KEY2=VALUE2

# List app settings
az webapp config appsettings list \
  --name primus-saas-api \
  --resource-group primus-saas-rg

# Configure CORS
az webapp cors add \
  --name primus-saas-api \
  --resource-group primus-saas-rg \
  --allowed-origins https://example.com

# Show CORS settings
az webapp cors show \
  --name primus-saas-api \
  --resource-group primus-saas-rg

# Stream logs
az webapp log tail \
  --name primus-saas-api \
  --resource-group primus-saas-rg

# Download logs
az webapp log download \
  --name primus-saas-api \
  --resource-group primus-saas-rg \
  --log-file logs.zip

# Enable managed identity
az webapp identity assign \
  --name primus-saas-api \
  --resource-group primus-saas-rg

# Show managed identity
az webapp identity show \
  --name primus-saas-api \
  --resource-group primus-saas-rg

# Scale App Service Plan
az appservice plan update \
  --name primus-saas-plan \
  --resource-group primus-saas-rg \
  --sku S1
```

## Static Web Apps

```bash
# Create Static Web App
az staticwebapp create \
  --name primus-saas-web \
  --resource-group primus-saas-rg \
  --location eastus \
  --source https://github.com/akkikhan/Primus-SaaS \
  --branch dev-9 \
  --app-location "/portal/frontend" \
  --output-location "dist" \
  --token GITHUB_TOKEN

# List Static Web Apps
az staticwebapp list --resource-group primus-saas-rg --output table

# Show Static Web App details
az staticwebapp show \
  --name primus-saas-web \
  --resource-group primus-saas-rg

# Get deployment token
az staticwebapp secrets list \
  --name primus-saas-web \
  --resource-group primus-saas-rg

# Add custom domain
az staticwebapp hostname set \
  --name primus-saas-web \
  --resource-group primus-saas-rg \
  --hostname app.example.com

# List hostnames
az staticwebapp hostname list \
  --name primus-saas-web \
  --resource-group primus-saas-rg
```

## Key Vault

```bash
# Create Key Vault
az keyvault create \
  --name primus-saas-kv-123 \
  --resource-group primus-saas-rg \
  --location eastus

# List Key Vaults
az keyvault list --resource-group primus-saas-rg --output table

# Set secret
az keyvault secret set \
  --vault-name primus-saas-kv-123 \
  --name SecretName \
  --value "SecretValue"

# Get secret
az keyvault secret show \
  --vault-name primus-saas-kv-123 \
  --name SecretName

# List secrets
az keyvault secret list \
  --vault-name primus-saas-kv-123 \
  --output table

# Delete secret
az keyvault secret delete \
  --vault-name primus-saas-kv-123 \
  --name SecretName

# Set access policy
az keyvault set-policy \
  --name primus-saas-kv-123 \
  --object-id OBJECT_ID \
  --secret-permissions get list set delete

# List access policies
az keyvault show \
  --name primus-saas-kv-123 \
  --resource-group primus-saas-rg \
  --query properties.accessPolicies
```

## Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app primus-saas-ai \
  --location eastus \
  --resource-group primus-saas-rg \
  --application-type web

# List Application Insights
az monitor app-insights component list \
  --resource-group primus-saas-rg \
  --output table

# Get instrumentation key
az monitor app-insights component show \
  --app primus-saas-ai \
  --resource-group primus-saas-rg \
  --query instrumentationKey

# Query logs (requires workspace-based App Insights)
az monitor app-insights query \
  --app primus-saas-ai \
  --resource-group primus-saas-rg \
  --analytics-query "requests | where timestamp > ago(1h) | summarize count() by resultCode"
```

## Service Principal (for CI/CD)

```bash
# Create service principal with contributor role
az ad sp create-for-rbac \
  --name "primus-saas-github-actions" \
  --role contributor \
  --scopes /subscriptions/SUBSCRIPTION_ID/resourceGroups/primus-saas-rg \
  --sdk-auth

# List service principals
az ad sp list --display-name "primus-saas-github-actions"

# Delete service principal
az ad sp delete --id APP_ID
```

## Monitoring & Diagnostics

```bash
# Get activity log
az monitor activity-log list \
  --resource-group primus-saas-rg \
  --max-events 20

# Get metrics for App Service
az monitor metrics list \
  --resource /subscriptions/SUB_ID/resourceGroups/primus-saas-rg/providers/Microsoft.Web/sites/primus-saas-api \
  --metric "CpuPercentage" \
  --start-time 2024-11-01T00:00:00Z \
  --end-time 2024-11-18T23:59:59Z

# Create alert rule
az monitor metrics alert create \
  --name high-cpu-alert \
  --resource-group primus-saas-rg \
  --scopes /subscriptions/SUB_ID/resourceGroups/primus-saas-rg/providers/Microsoft.Web/sites/primus-saas-api \
  --condition "avg Percentage CPU > 80" \
  --window-size 5m \
  --evaluation-frequency 1m

# List alert rules
az monitor metrics alert list \
  --resource-group primus-saas-rg \
  --output table
```

## Networking

```bash
# Show outbound IP addresses for App Service
az webapp show \
  --resource-group primus-saas-rg \
  --name primus-saas-api \
  --query outboundIpAddresses

# Configure virtual network integration (requires higher SKU)
az webapp vnet-integration add \
  --resource-group primus-saas-rg \
  --name primus-saas-api \
  --vnet MyVNet \
  --subnet MySubnet
```

## Backup & Restore

```bash
# Create storage account for backups
az storage account create \
  --name primusbackups \
  --resource-group primus-saas-rg \
  --location eastus \
  --sku Standard_LRS

# Configure automatic backups (requires Standard tier or higher)
az webapp config backup update \
  --resource-group primus-saas-rg \
  --webapp-name primus-saas-api \
  --container-url "STORAGE_SAS_URL" \
  --frequency 1d \
  --retain-one true \
  --retention 30

# List backups
az webapp config backup list \
  --resource-group primus-saas-rg \
  --webapp-name primus-saas-api

# Restore from backup
az webapp config backup restore \
  --resource-group primus-saas-rg \
  --webapp-name primus-saas-api \
  --backup-name BackupName \
  --container-url "STORAGE_SAS_URL"
```

## Cost Management

```bash
# Show cost analysis
az consumption usage list \
  --start-date 2024-11-01 \
  --end-date 2024-11-30

# Set budget
az consumption budget create \
  --budget-name monthly-budget \
  --resource-group primus-saas-rg \
  --amount 100 \
  --time-grain Monthly \
  --start-date 2024-11-01 \
  --end-date 2025-11-01
```

## Useful Flags

```bash
# Output formats
--output table          # Table format (human-readable)
--output json           # JSON format (default)
--output jsonc          # Colored JSON
--output tsv            # Tab-separated values
--output yaml           # YAML format
--output yamlc          # Colored YAML
--output none           # No output

# Common flags
--help                  # Show help
--verbose               # Verbose output
--debug                 # Debug output
--only-show-errors      # Only show errors
--no-wait               # Don't wait for operation to complete
--yes                   # Don't prompt for confirmation
```

## Query with JMESPath

```bash
# Get specific property
az webapp show --name primus-saas-api --resource-group primus-saas-rg --query defaultHostName

# Get multiple properties
az webapp show --name primus-saas-api --resource-group primus-saas-rg --query "{name:name, url:defaultHostName, state:state}"

# Filter results
az webapp list --resource-group primus-saas-rg --query "[?state=='Running'].{name:name, url:defaultHostName}"
```

## Batch Operations

```bash
# Delete multiple resources
az resource list --resource-group primus-saas-rg --query "[].id" -o tsv | xargs -I {} az resource delete --ids {}

# Update multiple app settings
az webapp config appsettings set \
  --name primus-saas-api \
  --resource-group primus-saas-rg \
  --settings @settings.json
```

## Environment-Specific Commands

### Development
```bash
az group create --name primus-saas-dev-rg --location eastus
az appservice plan create --name primus-saas-dev-plan --sku B1 --resource-group primus-saas-dev-rg
```

### Staging
```bash
az group create --name primus-saas-staging-rg --location eastus
az appservice plan create --name primus-saas-staging-plan --sku S1 --resource-group primus-saas-staging-rg
```

### Production
```bash
az group create --name primus-saas-prod-rg --location eastus
az appservice plan create --name primus-saas-prod-plan --sku P1V2 --resource-group primus-saas-prod-rg
```

## Troubleshooting Commands

```bash
# Check resource health
az resource show --ids /subscriptions/SUB_ID/resourceGroups/primus-saas-rg/providers/Microsoft.Web/sites/primus-saas-api

# Get deployment logs
az webapp log deployment show \
  --name primus-saas-api \
  --resource-group primus-saas-rg

# Test connectivity
az network watcher test-connectivity \
  --resource-group primus-saas-rg \
  --source-resource primus-saas-api \
  --dest-address primus-saas-sql.database.windows.net \
  --dest-port 1433
```

## References

- [Azure CLI Documentation](https://docs.microsoft.com/en-us/cli/azure/)
- [Azure CLI Reference](https://docs.microsoft.com/en-us/cli/azure/reference-index)
- [JMESPath Query Examples](https://jmespath.org/examples.html)

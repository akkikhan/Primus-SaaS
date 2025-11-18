<#
.SYNOPSIS
    Deploy Primus SaaS platform to Azure using Azure CLI
.DESCRIPTION
    This script creates all necessary Azure resources and deploys the Primus SaaS platform:
    - Resource Group
    - Azure SQL Database
    - App Service Plan and App Service for Backend API
    - Static Web App for Frontend
    - Key Vault for secrets management
    - Application Insights for monitoring
.PARAMETER ResourceGroup
    Name of the Azure resource group (default: primus-saas-rg)
.PARAMETER Location
    Azure region for resources (default: eastus)
.PARAMETER Environment
    Environment name (dev, staging, prod) (default: dev)
#>

param(
    [string]$ResourceGroup = "primus-saas-rg",
    [string]$Location = "eastus",
    [string]$Environment = "dev",
    [string]$SqlAdminUsername = "primusadmin",
    [switch]$SkipInfrastructure,
    [switch]$DeployOnly
)

$ErrorActionPreference = "Stop"

# Configuration
$appName = "primus-saas-$Environment"
$sqlServerName = "$appName-sql"
$sqlDatabaseName = "PrimusSaasDb"
$appServicePlanName = "$appName-plan"
$backendAppName = "$appName-api"
$frontendAppName = "$appName-web"
$keyVaultName = "$appName-kv-$(Get-Random -Maximum 9999)"
$appInsightsName = "$appName-ai"

Write-Host "=== Primus SaaS Azure Deployment ===" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Resource Group: $ResourceGroup" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host ""

# Check if Azure CLI is installed
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "✗ Azure CLI is not installed. Please install it from https://aka.ms/azure-cli" -ForegroundColor Red
    exit 1
}

# Check if logged in
Write-Host "`nChecking Azure login status..." -ForegroundColor Cyan
$accountInfo = az account show 2>$null
if (-not $accountInfo) {
    Write-Host "Not logged in. Please login to Azure..." -ForegroundColor Yellow
    az login
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Azure login failed" -ForegroundColor Red
        exit 1
    }
}

$account = az account show | ConvertFrom-Json
Write-Host "✓ Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "✓ Subscription: $($account.name) ($($account.id))" -ForegroundColor Green

if (-not $DeployOnly) {
    # Step 1: Create Resource Group
    Write-Host "`n[1/8] Creating Resource Group..." -ForegroundColor Cyan
    $rgExists = az group exists --name $ResourceGroup
    if ($rgExists -eq "true") {
        Write-Host "✓ Resource group '$ResourceGroup' already exists" -ForegroundColor Yellow
    } else {
        az group create --name $ResourceGroup --location $Location --output none
        Write-Host "✓ Resource group created: $ResourceGroup" -ForegroundColor Green
    }

    if (-not $SkipInfrastructure) {
        # Step 2: Create Azure SQL Server and Database
        Write-Host "`n[2/8] Creating Azure SQL Database..." -ForegroundColor Cyan
        
        # Generate a strong password for SQL admin
        $sqlAdminPassword = -join ((65..90) + (97..122) + (48..57) + (33, 35, 36, 37, 38, 42, 43, 45, 61, 63, 64) | Get-Random -Count 16 | ForEach-Object {[char]$_})
        $sqlAdminPassword += "Aa1!" # Ensure complexity requirements
        
        Write-Host "Creating SQL Server: $sqlServerName" -ForegroundColor White
        az sql server create `
            --name $sqlServerName `
            --resource-group $ResourceGroup `
            --location $Location `
            --admin-user $SqlAdminUsername `
            --admin-password $sqlAdminPassword `
            --enable-public-network true `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ SQL Server created successfully" -ForegroundColor Green
            
            # Configure firewall to allow Azure services
            Write-Host "Configuring SQL Server firewall..." -ForegroundColor White
            az sql server firewall-rule create `
                --resource-group $ResourceGroup `
                --server $sqlServerName `
                --name "AllowAzureServices" `
                --start-ip-address 0.0.0.0 `
                --end-ip-address 0.0.0.0 `
                --output none
            
            # Create database
            Write-Host "Creating database: $sqlDatabaseName" -ForegroundColor White
            az sql db create `
                --resource-group $ResourceGroup `
                --server $sqlServerName `
                --name $sqlDatabaseName `
                --service-objective S0 `
                --backup-storage-redundancy Local `
                --output none
            
            Write-Host "✓ Database created successfully" -ForegroundColor Green
        } else {
            Write-Host "! SQL Server might already exist or error occurred" -ForegroundColor Yellow
        }

        # Step 3: Create Key Vault
        Write-Host "`n[3/8] Creating Key Vault..." -ForegroundColor Cyan
        az keyvault create `
            --name $keyVaultName `
            --resource-group $ResourceGroup `
            --location $Location `
            --enable-rbac-authorization false `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Key Vault created: $keyVaultName" -ForegroundColor Green
            
            # Store SQL admin password in Key Vault
            Write-Host "Storing SQL credentials in Key Vault..." -ForegroundColor White
            az keyvault secret set `
                --vault-name $keyVaultName `
                --name "SqlAdminPassword" `
                --value $sqlAdminPassword `
                --output none
            
            # Generate and store JWT secret
            $jwtSecret = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
            az keyvault secret set `
                --vault-name $keyVaultName `
                --name "JwtSecret" `
                --value $jwtSecret `
                --output none
            
            Write-Host "✓ Secrets stored in Key Vault" -ForegroundColor Green
        } else {
            Write-Host "! Key Vault might already exist or error occurred" -ForegroundColor Yellow
        }

        # Step 4: Create Application Insights
        Write-Host "`n[4/8] Creating Application Insights..." -ForegroundColor Cyan
        az monitor app-insights component create `
            --app $appInsightsName `
            --location $Location `
            --resource-group $ResourceGroup `
            --application-type web `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Application Insights created" -ForegroundColor Green
        } else {
            Write-Host "! Application Insights might already exist or error occurred" -ForegroundColor Yellow
        }

        # Step 5: Create App Service Plan
        Write-Host "`n[5/8] Creating App Service Plan..." -ForegroundColor Cyan
        az appservice plan create `
            --name $appServicePlanName `
            --resource-group $ResourceGroup `
            --location $Location `
            --sku B1 `
            --is-linux `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ App Service Plan created" -ForegroundColor Green
        } else {
            Write-Host "! App Service Plan might already exist or error occurred" -ForegroundColor Yellow
        }

        # Step 6: Create App Service for Backend API
        Write-Host "`n[6/8] Creating App Service for Backend API..." -ForegroundColor Cyan
        az webapp create `
            --name $backendAppName `
            --resource-group $ResourceGroup `
            --plan $appServicePlanName `
            --runtime "DOTNET|7.0" `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Backend App Service created: $backendAppName" -ForegroundColor Green
            
            # Get SQL connection string
            $sqlConnectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Initial Catalog=$sqlDatabaseName;Persist Security Info=False;User ID=$SqlAdminUsername;Password=$sqlAdminPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
            
            # Get Application Insights key
            $aiKey = az monitor app-insights component show --app $appInsightsName --resource-group $ResourceGroup --query instrumentationKey -o tsv 2>$null
            
            # Configure app settings
            Write-Host "Configuring Backend App Settings..." -ForegroundColor White
            az webapp config appsettings set `
                --name $backendAppName `
                --resource-group $ResourceGroup `
                --settings `
                    "ASPNETCORE_ENVIRONMENT=$Environment" `
                    "ConnectionStrings__DefaultConnection=$sqlConnectionString" `
                    "JwtSettings__Secret=@Microsoft.KeyVault(SecretUri=https://$keyVaultName.vault.azure.net/secrets/JwtSecret/)" `
                    "JwtSettings__Issuer=https://$backendAppName.azurewebsites.net" `
                    "JwtSettings__Audience=https://$backendAppName.azurewebsites.net" `
                    "JwtSettings__ExpiryMinutes=60" `
                    "APPLICATIONINSIGHTS_CONNECTION_STRING=$(if($aiKey){"InstrumentationKey=$aiKey"})" `
                --output none
            
            # Enable managed identity
            Write-Host "Enabling Managed Identity..." -ForegroundColor White
            $principalId = az webapp identity assign --name $backendAppName --resource-group $ResourceGroup --query principalId -o tsv
            
            # Grant Key Vault access to managed identity
            if ($principalId) {
                az keyvault set-policy `
                    --name $keyVaultName `
                    --object-id $principalId `
                    --secret-permissions get list `
                    --output none
                Write-Host "✓ Managed Identity configured with Key Vault access" -ForegroundColor Green
            }
            
            # Configure CORS for frontend
            Write-Host "Configuring CORS..." -ForegroundColor White
            az webapp cors add `
                --name $backendAppName `
                --resource-group $ResourceGroup `
                --allowed-origins "https://$frontendAppName.azurestaticapps.net" "http://localhost:5173" `
                --output none
            
            Write-Host "✓ Backend configuration complete" -ForegroundColor Green
        } else {
            Write-Host "! Backend App Service might already exist or error occurred" -ForegroundColor Yellow
        }

        # Step 7: Create Static Web App for Frontend
        Write-Host "`n[7/8] Creating Static Web App for Frontend..." -ForegroundColor Cyan
        Write-Host "Note: Static Web Apps require GitHub integration for automatic deployment." -ForegroundColor Yellow
        Write-Host "You can create it manually at: https://portal.azure.com/#create/Microsoft.StaticApp" -ForegroundColor Yellow
        
        # Alternative: Create using CLI (requires GitHub token)
        Write-Host "`nTo create Static Web App with CLI, you need:" -ForegroundColor White
        Write-Host "  1. GitHub Personal Access Token" -ForegroundColor White
        Write-Host "  2. Repository URL: https://github.com/akkikhan/Primus-SaaS" -ForegroundColor White
        Write-Host "  3. Branch: dev-9" -ForegroundColor White
        Write-Host "`nSkipping automatic creation. Please create manually or run:" -ForegroundColor Yellow
        Write-Host "  az staticwebapp create --name $frontendAppName --resource-group $ResourceGroup --location $Location --source https://github.com/akkikhan/Primus-SaaS --branch dev-9 --app-location '/portal/frontend' --api-location '' --output-location 'dist' --token <YOUR_GITHUB_TOKEN>" -ForegroundColor Gray
    }
}

# Step 8: Deployment summary
Write-Host "`n[8/8] Deployment Summary" -ForegroundColor Cyan
Write-Host "=============================================`n" -ForegroundColor Cyan

$resources = @"
Resource Group:         $ResourceGroup
Location:               $Location
Environment:            $Environment

Backend API:
  App Service:          $backendAppName
  URL:                  https://$backendAppName.azurewebsites.net
  Plan:                 $appServicePlanName

Database:
  SQL Server:           $sqlServerName.database.windows.net
  Database:             $sqlDatabaseName
  Admin User:           $SqlAdminUsername

Frontend:
  Static Web App:       $frontendAppName (manual setup required)

Security:
  Key Vault:            $keyVaultName
  Managed Identity:     Enabled on Backend API

Monitoring:
  App Insights:         $appInsightsName

Next Steps:
1. Deploy backend code:
   cd portal/backend
   dotnet publish -c Release
   az webapp deploy --resource-group $ResourceGroup --name $backendAppName --src-path ./bin/Release/net7.0/publish --type zip

2. Run database migrations:
   Update connection string in appsettings.json
   dotnet ef database update

3. Create Static Web App manually:
   https://portal.azure.com/#create/Microsoft.StaticApp
   - Connect to GitHub repository
   - Set app location: /portal/frontend
   - Set output location: dist
   - Configure API URL in frontend environment

4. Update frontend API endpoint:
   Create .env.production file with:
   VITE_API_URL=https://$backendAppName.azurewebsites.net

5. View resources in Azure Portal:
   https://portal.azure.com/#@/resource/subscriptions/$($account.id)/resourceGroups/$ResourceGroup
"@

Write-Host $resources -ForegroundColor White

Write-Host "`n✓ Deployment script completed!" -ForegroundColor Green
Write-Host "`nIMPORTANT: Save these credentials securely:" -ForegroundColor Yellow
Write-Host "  SQL Admin Password is stored in Key Vault: $keyVaultName/secrets/SqlAdminPassword" -ForegroundColor Yellow
Write-Host "  JWT Secret is stored in Key Vault: $keyVaultName/secrets/JwtSecret" -ForegroundColor Yellow

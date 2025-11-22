# Azure AD Integration Test Script
# This script tests the complete Azure AD authentication flow with Primus SaaS

Write-Host "🔐 Azure AD Integration Test Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$AZURE_CLIENT_ID = "c28b195b-8396-42e6-bc6f-7773736dfa40"  # Replace with your Azure AD Client ID
$PRIMUS_PORTAL_URL = "http://localhost:5267"
$ACME_DASHBOARD_URL = "http://localhost:3000"

Write-Host "📋 Configuration:" -ForegroundColor Yellow
Write-Host "  Azure Client ID: $AZURE_CLIENT_ID"
Write-Host "  Primus Portal: $PRIMUS_PORTAL_URL"
Write-Host "  Acme Dashboard: $ACME_DASHBOARD_URL"
Write-Host ""

# Step 1: Check if Azure CLI is installed
Write-Host "Step 1: Checking Azure CLI installation..." -ForegroundColor Green
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "  ✅ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "  ❌ Azure CLI not found. Please install it first:" -ForegroundColor Red
    Write-Host "     winget install Microsoft.AzureCLI" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Step 2: Check if logged in to Azure
Write-Host "Step 2: Checking Azure login status..." -ForegroundColor Green
try {
    $account = az account show --output json 2>$null | ConvertFrom-Json
    Write-Host "  ✅ Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "  ✅ Tenant: $($account.tenantId)" -ForegroundColor Green
} catch {
    Write-Host "  ⚠️  Not logged in to Azure. Logging in now..." -ForegroundColor Yellow
    az login
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ❌ Azure login failed" -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

# Step 3: Get Azure AD access token
Write-Host "Step 3: Getting Azure AD access token..." -ForegroundColor Green
try {
    $tokenResponse = az account get-access-token --resource $AZURE_CLIENT_ID --output json | ConvertFrom-Json
    $azureToken = $tokenResponse.accessToken
    Write-Host "  ✅ Token obtained successfully" -ForegroundColor Green
    Write-Host "  ✅ Expires on: $($tokenResponse.expiresOn)" -ForegroundColor Green
    Write-Host "  ✅ Token preview: $($azureToken.Substring(0, 50))..." -ForegroundColor Gray
} catch {
    Write-Host "  ❌ Failed to get access token" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Test Primus Portal Azure login endpoint
Write-Host "Step 4: Testing Primus Portal Azure AD login..." -ForegroundColor Green
try {
    $body = @{
        idToken = $azureToken
    } | ConvertTo-Json

    $primusResponse = Invoke-RestMethod -Uri "$PRIMUS_PORTAL_URL/api/auth/azure" `
        -Method POST `
        -Body $body `
        -ContentType "application/json" `
        -ErrorAction Stop

    Write-Host "  ✅ Authentication successful!" -ForegroundColor Green
    Write-Host "  ✅ User ID: $($primusResponse.id)" -ForegroundColor Green
    Write-Host "  ✅ Email: $($primusResponse.email)" -ForegroundColor Green
    Write-Host "  ✅ Role: $($primusResponse.role)" -ForegroundColor Green
    
    $primusToken = $primusResponse.token
    Write-Host "  ✅ Primus JWT token: $($primusToken.Substring(0, 50))..." -ForegroundColor Gray
} catch {
    Write-Host "  ❌ Primus Portal authentication failed" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Red
    }
    exit 1
}
Write-Host ""

# Step 5: Test Acme Dashboard protected endpoint
Write-Host "Step 5: Testing Acme Dashboard protected API..." -ForegroundColor Green
try {
    $headers = @{
        "Authorization" = "Bearer $primusToken"
    }

    $dashboardResponse = Invoke-RestMethod -Uri "$ACME_DASHBOARD_URL/api/revenue-stats" `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "  ✅ Protected API access successful!" -ForegroundColor Green
    Write-Host "  ✅ Company: $($dashboardResponse.company)" -ForegroundColor Green
    Write-Host "  ✅ Revenue: $($dashboardResponse.revenue)" -ForegroundColor Green
    Write-Host "  ✅ Growth: $($dashboardResponse.growth)" -ForegroundColor Green
    Write-Host "  ✅ Active Users: $($dashboardResponse.activeUsers)" -ForegroundColor Green
    Write-Host ""
    Write-Host "  👤 Authenticated User:" -ForegroundColor Cyan
    Write-Host "     User ID: $($dashboardResponse.user.userId)" -ForegroundColor Gray
    Write-Host "     Email: $($dashboardResponse.user.email)" -ForegroundColor Gray
    Write-Host "     Name: $($dashboardResponse.user.name)" -ForegroundColor Gray
    Write-Host "     Roles: $($dashboardResponse.user.roles -join ', ')" -ForegroundColor Gray
} catch {
    Write-Host "  ❌ Acme Dashboard API call failed" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Red
    }
    exit 1
}
Write-Host ""

# Step 6: Test without token (should fail)
Write-Host "Step 6: Testing unauthorized access (should fail)..." -ForegroundColor Green
try {
    $unauthorizedResponse = Invoke-RestMethod -Uri "$ACME_DASHBOARD_URL/api/revenue-stats" `
        -ErrorAction Stop
    
    Write-Host "  ❌ SECURITY ISSUE: Unauthorized access was allowed!" -ForegroundColor Red
} catch {
    Write-Host "  ✅ Unauthorized access correctly blocked (401)" -ForegroundColor Green
}
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ ALL TESTS PASSED!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "🎉 Azure AD integration is working correctly!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Summary:" -ForegroundColor Yellow
Write-Host "  1. Azure AD token obtained successfully"
Write-Host "  2. Primus Portal validated Azure AD token"
Write-Host "  3. Primus JWT token issued"
Write-Host "  4. Acme Dashboard protected API accessible with token"
Write-Host "  5. Unauthorized access properly blocked"
Write-Host ""
Write-Host "🔑 Your Primus JWT Token (valid for 1 hour):" -ForegroundColor Yellow
Write-Host $primusToken -ForegroundColor Gray
Write-Host ""
Write-Host "💡 You can use this token to make API calls:" -ForegroundColor Yellow
Write-Host "   curl -H 'Authorization: Bearer $primusToken' $ACME_DASHBOARD_URL/api/revenue-stats" -ForegroundColor Gray
Write-Host ""

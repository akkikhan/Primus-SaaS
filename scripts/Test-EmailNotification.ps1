# Test Email Notification via Primus Portal API
# This script tests the full notification stack: queue -> template -> SMTP

param(
    [string]$ApiBaseUrl = "http://localhost:5267",
    [string]$RecipientEmail = "test@example.com"
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Primus SaaS - Email Notification Test" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# First, login to get a token
Write-Host "1. Authenticating..." -ForegroundColor Yellow

$loginBody = @{
    email = "admin@primus.com"
    password = "Admin123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "   ✓ Authenticated as $($loginResponse.email)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Authentication failed: $_" -ForegroundColor Red
    Write-Host "   Make sure the portal backend is running on $ApiBaseUrl" -ForegroundColor Yellow
    exit 1
}

# Create headers with auth token
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test 1: Check notification preferences endpoint
Write-Host "`n2. Testing Notification Preferences API..." -ForegroundColor Yellow
try {
    $prefs = Invoke-RestMethod -Uri "$ApiBaseUrl/api/notificationpreferences" -Method GET -Headers $headers
    Write-Host "   ✓ Preferences API working" -ForegroundColor Green
} catch {
    Write-Host "   ⚠ Preferences API: $_" -ForegroundColor Yellow
}

# Test 2: Create a test application (this triggers email notification)
Write-Host "`n3. Creating test application (triggers email)..." -ForegroundColor Yellow

$appBody = @{
    name = "Email Test App $(Get-Date -Format 'HHmmss')"
    stack = "DotNet"
    description = "Test application to verify email notifications"
    clientEmail = $RecipientEmail
} | ConvertTo-Json

try {
    $appResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/applications" -Method POST -Body $appBody -Headers $headers
    Write-Host "   ✓ Application created: $($appResponse.name)" -ForegroundColor Green
    Write-Host "   ✓ Client ID: $($appResponse.primusClientId)" -ForegroundColor Green
    Write-Host "   → Email notification should be sent to: $RecipientEmail" -ForegroundColor Cyan
} catch {
    Write-Host "   ✗ Failed to create application: $_" -ForegroundColor Red
    
    # Try to get more details
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "   Response: $responseBody" -ForegroundColor Gray
    }
}

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "  Test Complete" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "If the test SMTP server is running (Start-TestSmtpServer.ps1)," -ForegroundColor Yellow
Write-Host "you should see the email content in that terminal window." -ForegroundColor Yellow

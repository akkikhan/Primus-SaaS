# Test Webhook Audit Trail
# Tests that all webhook requests are being logged to WebhookRequests table

param(
    [string]$ApiUrl = "http://localhost:5267",
    [string]$NpmSecret = "your-npm-webhook-secret-here",
    [string]$NuGetSecret = "your-nuget-webhook-secret-here",
    [string]$AdminEmail = "admin@primussaas.com",
    [string]$AdminPassword = "Admin123!"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Webhook Audit Trail Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login as admin to get auth token
Write-Host "Step 1: Logging in as admin..." -ForegroundColor Cyan

$loginBody = @{
    Email = $AdminEmail
    Password = $AdminPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$ApiUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✅ Login successful" -ForegroundColor Green
}
catch {
    Write-Host "❌ Login failed: $_" -ForegroundColor Red
    Write-Host "Please ensure the API is running and admin credentials are correct" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Step 2: Get initial webhook count
Write-Host "Step 2: Getting initial webhook count..." -ForegroundColor Cyan

$headers = @{
    Authorization = "Bearer $token"
}

try {
    $historyBefore = Invoke-RestMethod -Uri "$ApiUrl/api/webhooks/history" -Headers $headers
    $initialCount = $historyBefore.total
    Write-Host "✅ Initial webhook count: $initialCount" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to get webhook history: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Send test webhooks
Write-Host "Step 3: Sending test webhooks..." -ForegroundColor Cyan

$testResults = @{
    SuccessfulRequests = 0
    FailedRequests = 0
}

# Test 3a: Valid npm webhook
Write-Host "  3a: Valid npm webhook..." -ForegroundColor White

$npmPayload = @{
    event = "package:publish"
    name = "primus-identity-validator"
    version = "1.5.0"
    time = [int](Get-Date -UFormat %s)
    change = @{
        dist = @{
            tarball = "https://registry.npmjs.org/primus-identity-validator/-/primus-identity-validator-1.5.0.tgz"
        }
    }
} | ConvertTo-Json -Compress

$hmacNpm = New-Object System.Security.Cryptography.HMACSHA256
$hmacNpm.Key = [System.Text.Encoding]::UTF8.GetBytes($NpmSecret)
$hashNpm = $hmacNpm.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($npmPayload))
$signatureNpm = "sha256=" + [BitConverter]::ToString($hashNpm).Replace("-", "").ToLower()

try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/webhooks/npm-registry" -Method Post -Body $npmPayload -Headers @{
        "Content-Type" = "application/json"
        "X-Npm-Signature" = $signatureNpm
    } -ErrorAction Stop
    Write-Host "     ✅ Valid npm webhook: $($response.StatusCode)" -ForegroundColor Green
    $testResults.SuccessfulRequests++
}
catch {
    Write-Host "     ❌ Failed: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    $testResults.FailedRequests++
}

Start-Sleep -Milliseconds 100

# Test 3b: Invalid signature npm webhook
Write-Host "  3b: Invalid signature npm webhook..." -ForegroundColor White

try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/webhooks/npm-registry" -Method Post -Body $npmPayload -Headers @{
        "Content-Type" = "application/json"
        "X-Npm-Signature" = "sha256=invalid_signature_here"
    } -ErrorAction Stop
    Write-Host "     ⚠️  Unexpected success: $($response.StatusCode)" -ForegroundColor Yellow
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 401) {
        Write-Host "     ✅ Invalid signature rejected: 401" -ForegroundColor Green
        $testResults.SuccessfulRequests++
    }
    else {
        Write-Host "     ⚠️  Unexpected status: $statusCode" -ForegroundColor Yellow
        $testResults.FailedRequests++
    }
}

Start-Sleep -Milliseconds 100

# Test 3c: Wrong event type npm webhook
Write-Host "  3c: Wrong event type npm webhook..." -ForegroundColor White

$npmPayloadWrongEvent = @{
    event = "package:unpublish"
    name = "primus-identity-validator"
    version = "1.5.0"
    time = [int](Get-Date -UFormat %s)
} | ConvertTo-Json -Compress

$hmacNpm2 = New-Object System.Security.Cryptography.HMACSHA256
$hmacNpm2.Key = [System.Text.Encoding]::UTF8.GetBytes($NpmSecret)
$hashNpm2 = $hmacNpm2.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($npmPayloadWrongEvent))
$signatureNpm2 = "sha256=" + [BitConverter]::ToString($hashNpm2).Replace("-", "").ToLower()

try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/webhooks/npm-registry" -Method Post -Body $npmPayloadWrongEvent -Headers @{
        "Content-Type" = "application/json"
        "X-Npm-Signature" = $signatureNpm2
    } -ErrorAction Stop
    Write-Host "     ✅ Wrong event ignored: $($response.StatusCode)" -ForegroundColor Green
    $testResults.SuccessfulRequests++
}
catch {
    Write-Host "     ❌ Failed: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    $testResults.FailedRequests++
}

Start-Sleep -Milliseconds 100

# Test 3d: Valid NuGet webhook
Write-Host "  3d: Valid NuGet webhook..." -ForegroundColor White

$nugetPayload = @{
    Event = "PackagePushed"
    PackageId = "PrimusSaaS.Identity.Validator"
    Version = "1.5.0"
    Published = (Get-Date).ToUniversalTime().ToString("o")
    Metadata = @{
        Urls = @{
            PackageDownload = "https://api.nuget.org/v3-flatcontainer/primussaas.identity.validator/1.5.0/primussaas.identity.validator.1.5.0.nupkg"
        }
    }
} | ConvertTo-Json -Compress

$hmacNuGet = New-Object System.Security.Cryptography.HMACSHA256
$hmacNuGet.Key = [System.Text.Encoding]::UTF8.GetBytes($NuGetSecret)
$hashNuGet = $hmacNuGet.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($nugetPayload))
$signatureNuGet = "sha256=" + [BitConverter]::ToString($hashNuGet).Replace("-", "").ToLower()

try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/webhooks/nuget-registry" -Method Post -Body $nugetPayload -Headers @{
        "Content-Type" = "application/json"
        "X-NuGet-Signature" = $signatureNuGet
    } -ErrorAction Stop
    Write-Host "     ✅ Valid NuGet webhook: $($response.StatusCode)" -ForegroundColor Green
    $testResults.SuccessfulRequests++
}
catch {
    Write-Host "     ❌ Failed: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    $testResults.FailedRequests++
}

Write-Host ""

# Step 4: Query webhook history
Write-Host "Step 4: Querying webhook history..." -ForegroundColor Cyan

Start-Sleep -Milliseconds 500

try {
    $historyAfter = Invoke-RestMethod -Uri "$ApiUrl/api/webhooks/history" -Headers $headers
    $newWebhooks = $historyAfter.total - $initialCount
    
    Write-Host "✅ Total webhooks after test: $($historyAfter.total)" -ForegroundColor Green
    Write-Host "✅ New webhooks logged: $newWebhooks" -ForegroundColor Green
    
    if ($newWebhooks -ge 4) {
        Write-Host "✅ All test webhooks were logged (expected 4, got $newWebhooks)" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  Some webhooks may not have been logged (expected 4, got $newWebhooks)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Failed to query webhook history: $_" -ForegroundColor Red
}

Write-Host ""

# Step 5: Test filtering
Write-Host "Step 5: Testing webhook history filters..." -ForegroundColor Cyan

# Test 5a: Filter by registry type
Write-Host "  5a: Filter by registryType=npm..." -ForegroundColor White
try {
    $npmHistory = Invoke-RestMethod -Uri "$ApiUrl/api/webhooks/history?registryType=npm" -Headers $headers
    Write-Host "     ✅ Found $($npmHistory.total) npm webhooks" -ForegroundColor Green
}
catch {
    Write-Host "     ❌ Filter failed: $_" -ForegroundColor Red
}

# Test 5b: Filter by package name
Write-Host "  5b: Filter by packageName=primus-identity-validator..." -ForegroundColor White
try {
    $packageHistory = Invoke-RestMethod -Uri "$ApiUrl/api/webhooks/history?packageName=primus-identity-validator" -Headers $headers
    Write-Host "     ✅ Found $($packageHistory.total) webhooks for primus-identity-validator" -ForegroundColor Green
}
catch {
    Write-Host "     ❌ Filter failed: $_" -ForegroundColor Red
}

# Test 5c: Filter by status code
Write-Host "  5c: Filter by statusCode=401 (unauthorized)..." -ForegroundColor White
try {
    $unauthorizedHistory = Invoke-RestMethod -Uri "$ApiUrl/api/webhooks/history?statusCode=401" -Headers $headers
    Write-Host "     ✅ Found $($unauthorizedHistory.total) unauthorized webhooks" -ForegroundColor Green
}
catch {
    Write-Host "     ❌ Filter failed: $_" -ForegroundColor Red
}

# Test 5d: Filter by date range (last hour)
Write-Host "  5d: Filter by date range (last hour)..." -ForegroundColor White
try {
    $startDate = (Get-Date).AddHours(-1).ToUniversalTime().ToString("o")
    $endDate = (Get-Date).ToUniversalTime().ToString("o")
    $dateHistory = Invoke-RestMethod -Uri "$ApiUrl/api/webhooks/history?startDate=$startDate&endDate=$endDate" -Headers $headers
    Write-Host "     ✅ Found $($dateHistory.total) webhooks in last hour" -ForegroundColor Green
}
catch {
    Write-Host "     ❌ Filter failed: $_" -ForegroundColor Red
}

# Test 5e: Pagination
Write-Host "  5e: Test pagination (page 1, pageSize 2)..." -ForegroundColor White
try {
    $paginatedHistory = Invoke-RestMethod -Uri "$ApiUrl/api/webhooks/history?page=1&pageSize=2" -Headers $headers
    Write-Host "     ✅ Retrieved page 1 with $($paginatedHistory.webhooks.Count) webhooks (total: $($paginatedHistory.total), pages: $($paginatedHistory.totalPages))" -ForegroundColor Green
}
catch {
    Write-Host "     ❌ Pagination failed: $_" -ForegroundColor Red
}

Write-Host ""

# Step 6: Verify webhook details
Write-Host "Step 6: Verifying webhook details..." -ForegroundColor Cyan

try {
    $recentWebhooks = Invoke-RestMethod -Uri "$ApiUrl/api/webhooks/history?page=1&pageSize=5" -Headers $headers
    
    if ($recentWebhooks.webhooks.Count -gt 0) {
        $webhook = $recentWebhooks.webhooks[0]
        
        Write-Host "  Most recent webhook details:" -ForegroundColor White
        Write-Host "    ID: $($webhook.id)" -ForegroundColor Gray
        Write-Host "    Endpoint: $($webhook.endpoint)" -ForegroundColor Gray
        Write-Host "    Registry: $($webhook.registryType)" -ForegroundColor Gray
        Write-Host "    Event: $($webhook.eventType)" -ForegroundColor Gray
        Write-Host "    Package: $($webhook.packageName) v$($webhook.packageVersion)" -ForegroundColor Gray
        Write-Host "    IP Address: $($webhook.ipAddress)" -ForegroundColor Gray
        Write-Host "    Status Code: $($webhook.statusCode)" -ForegroundColor Gray
        Write-Host "    Signature Valid: $($webhook.signatureValid)" -ForegroundColor Gray
        Write-Host "    Processing Time: $($webhook.processingTimeMs)ms" -ForegroundColor Gray
        Write-Host "    Created At: $($webhook.createdAt)" -ForegroundColor Gray
        
        # Verify required fields are populated
        $fieldsOk = $true
        
        if ([string]::IsNullOrEmpty($webhook.endpoint)) {
            Write-Host "    ❌ Endpoint is empty" -ForegroundColor Red
            $fieldsOk = $false
        }
        if ([string]::IsNullOrEmpty($webhook.registryType)) {
            Write-Host "    ❌ RegistryType is empty" -ForegroundColor Red
            $fieldsOk = $false
        }
        if ([string]::IsNullOrEmpty($webhook.ipAddress)) {
            Write-Host "    ❌ IpAddress is empty" -ForegroundColor Red
            $fieldsOk = $false
        }
        if ($webhook.statusCode -eq 0) {
            Write-Host "    ❌ StatusCode is not set" -ForegroundColor Red
            $fieldsOk = $false
        }
        if ($webhook.processingTimeMs -lt 0) {
            Write-Host "    ❌ ProcessingTimeMs is invalid" -ForegroundColor Red
            $fieldsOk = $false
        }
        
        if ($fieldsOk) {
            Write-Host "  ✅ All required fields are populated correctly" -ForegroundColor Green
        }
        else {
            Write-Host "  ⚠️  Some fields are missing or invalid" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "  ⚠️  No webhooks found to verify" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  ❌ Failed to retrieve webhook details: $_" -ForegroundColor Red
}

Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Successful Requests: $($testResults.SuccessfulRequests)" -ForegroundColor Green
Write-Host "  Failed Requests: $($testResults.FailedRequests)" -ForegroundColor Red
Write-Host "  Webhooks Logged: $newWebhooks" -ForegroundColor Green
Write-Host ""

if ($testResults.FailedRequests -eq 0 -and $newWebhooks -ge 4) {
    Write-Host "✅ PASS: Audit trail is working correctly!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "⚠️  PARTIAL: Some tests did not pass as expected" -ForegroundColor Yellow
    exit 1
}

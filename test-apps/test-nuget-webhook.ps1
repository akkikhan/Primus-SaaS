# Test NuGet webhook endpoint with HMAC signature validation

$secret = "your-nuget-webhook-secret-here"
$url = "http://localhost:5267/api/webhooks/nuget-registry"

# Test payload - NuGet package pushed event
$payload = @{
    Event = "PackagePushed"
    PackageId = "PrimusSaaS.Identity.Validator"
    Version = "1.2.0"
    PackageType = "Dependency"
    Published = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    Metadata = @{
        Description = "Identity validation module for Primus SaaS"
        Authors = @("Primus SaaS Team")
        Tags = @("identity", "validation", "jwt", "azuread")
        ProjectUrl = "https://github.com/akkikhan/Primus-SaaS"
        RepositoryUrl = "https://github.com/akkikhan/Primus-SaaS"
        Urls = @{
            PackageDetails = "https://www.nuget.org/packages/PrimusSaaS.Identity.Validator/1.2.0"
            PackageDownload = "https://www.nuget.org/api/v2/package/PrimusSaaS.Identity.Validator/1.2.0"
        }
    }
} | ConvertTo-Json -Depth 10

Write-Host "Payload:" -ForegroundColor Cyan
Write-Host $payload
Write-Host ""

# Generate HMAC-SHA256 signature
$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($secret)
$hash = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($payload))
$signature = "sha256=" + [BitConverter]::ToString($hash).Replace("-", "").ToLower()

Write-Host "Signature: $signature" -ForegroundColor Yellow
Write-Host ""

# Test 1: GET /api/webhooks/test
Write-Host "Test 1: GET /api/webhooks/test" -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5267/api/webhooks/test" -Method Get
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
Write-Host ""

# Test 2: POST with valid signature
Write-Host "Test 2: POST /api/webhooks/nuget-registry (valid signature)" -ForegroundColor Green
try {
    $headers = @{
        "Content-Type" = "application/json"
        "X-NuGet-Signature" = $signature
    }
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $payload -Headers $headers
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Red
    }
}
Write-Host ""

# Test 3: POST with invalid signature
Write-Host "Test 3: POST /api/webhooks/nuget-registry (invalid signature)" -ForegroundColor Green
try {
    $headers = @{
        "Content-Type" = "application/json"
        "X-NuGet-Signature" = "sha256=invalid"
    }
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $payload -Headers $headers
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "Expected Error (401 Unauthorized): $_" -ForegroundColor Yellow
}
Write-Host ""

# Test 4: POST with different event type
Write-Host "Test 4: POST /api/webhooks/nuget-registry (non-push event)" -ForegroundColor Green
$payload2 = @{
    Event = "PackageDeleted"
    PackageId = "PrimusSaaS.Identity.Validator"
    Version = "1.2.0"
    PackageType = "Dependency"
    Published = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
} | ConvertTo-Json

$hmac2 = New-Object System.Security.Cryptography.HMACSHA256
$hmac2.Key = [System.Text.Encoding]::UTF8.GetBytes($secret)
$hash2 = $hmac2.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($payload2))
$signature2 = "sha256=" + [BitConverter]::ToString($hash2).Replace("-", "").ToLower()

try {
    $headers = @{
        "Content-Type" = "application/json"
        "X-NuGet-Signature" = $signature2
    }
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $payload2 -Headers $headers
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
}

# Test 5: Test duplicate version (run npm webhook first to create version 1.0.2, then try again)
Write-Host ""
Write-Host "Test 5: POST /api/webhooks/nuget-registry (duplicate version test)" -ForegroundColor Green
Write-Host "Testing with same version again..." -ForegroundColor Cyan
try {
    $headers = @{
        "Content-Type" = "application/json"
        "X-NuGet-Signature" = $signature
    }
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $payload -Headers $headers
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Tests completed!" -ForegroundColor Cyan
Write-Host ""
Write-Host "To verify in database, run:" -ForegroundColor Yellow
Write-Host "sqlite3 portal.db `"SELECT * FROM ModuleVersions WHERE Version='1.2.0';`"" -ForegroundColor White

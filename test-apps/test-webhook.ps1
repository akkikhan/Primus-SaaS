# Test npm webhook endpoint with HMAC signature validation

$secret = "your-npm-webhook-secret-here"
$url = "http://localhost:5267/api/webhooks/npm-registry"

# Test payload - npm publish event
$payload = @{
    event = "package:publish"
    name = "primus-identity-validator"
    version = "1.0.2"
    type = "package"
    time = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    change = @{
        dist = @{
            tarball = "https://registry.npmjs.org/primus-identity-validator/-/primus-identity-validator-1.0.2.tgz"
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
Write-Host "Test 2: POST /api/webhooks/npm-registry (valid signature)" -ForegroundColor Green
try {
    $headers = @{
        "Content-Type" = "application/json"
        "X-Npm-Signature" = $signature
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
Write-Host "Test 3: POST /api/webhooks/npm-registry (invalid signature)" -ForegroundColor Green
try {
    $headers = @{
        "Content-Type" = "application/json"
        "X-Npm-Signature" = "sha256=invalid"
    }
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $payload -Headers $headers
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "Expected Error (401 Unauthorized): $_" -ForegroundColor Yellow
}
Write-Host ""

# Test 4: POST with different event type
Write-Host "Test 4: POST /api/webhooks/npm-registry (non-publish event)" -ForegroundColor Green
$payload2 = @{
    event = "package:update"
    name = "primus-identity-validator"
    version = "1.0.2"
    type = "package"
    time = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
} | ConvertTo-Json

$hmac2 = New-Object System.Security.Cryptography.HMACSHA256
$hmac2.Key = [System.Text.Encoding]::UTF8.GetBytes($secret)
$hash2 = $hmac2.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($payload2))
$signature2 = "sha256=" + [BitConverter]::ToString($hash2).Replace("-", "").ToLower()

try {
    $headers = @{
        "Content-Type" = "application/json"
        "X-Npm-Signature" = $signature2
    }
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $payload2 -Headers $headers
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Tests completed!" -ForegroundColor Cyan

# Test Script for Primus Auth Test Application
# Run this after starting the server with: npm run dev

Write-Host "==============================================================" -ForegroundColor Cyan
Write-Host "Testing Primus Auth Test Application" -ForegroundColor Cyan
Write-Host "==============================================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Public endpoint (no auth required)
Write-Host "Test 1: Public Endpoint (No Auth)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:3000/api/public" -Method GET
    Write-Host "✅ SUCCESS:" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "❌ FAILED: $_" -ForegroundColor Red
}
Write-Host ""

# Test 2: Health endpoint
Write-Host "Test 2: Health Endpoint" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:3000/api/health" -Method GET
    Write-Host "✅ SUCCESS:" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "❌ FAILED: $_" -ForegroundColor Red
}
Write-Host ""

# Test 3: Root endpoint
Write-Host "Test 3: Root Endpoint (Application Info)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:3000/" -Method GET
    Write-Host "✅ SUCCESS:" -ForegroundColor Green
    Write-Host "Message: $($response.message)"
    Write-Host "Version: $($response.version)"
    Write-Host "Validation Mode: $($response.configuration.validationMode)"
} catch {
    Write-Host "❌ FAILED: $_" -ForegroundColor Red
}
Write-Host ""

# Test 4: Protected endpoint without token (should fail with 401)
Write-Host "Test 4: Protected Endpoint Without Token (Should Fail)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:3000/api/user/profile" -Method GET
    Write-Host "❌ UNEXPECTED SUCCESS (should have been 401)" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✅ EXPECTED FAILURE: 401 Unauthorized" -ForegroundColor Green
    } else {
        Write-Host "❌ UNEXPECTED ERROR: $_" -ForegroundColor Red
    }
}
Write-Host ""

Write-Host "==============================================================" -ForegroundColor Cyan
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Generate a test token: node generate-test-token.js" -ForegroundColor White
Write-Host "2. Test with token: See test-with-token.ps1" -ForegroundColor White
Write-Host "==============================================================" -ForegroundColor Cyan

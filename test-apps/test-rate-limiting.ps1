# Test Rate Limiting for Webhook Endpoints
# Tests that rate limiting (100 req/min) is working correctly

param(
    [string]$Url = "http://localhost:5267/api/webhooks/npm-registry",
    [string]$Secret = "your-npm-webhook-secret-here",
    [int]$TotalRequests = 150
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Webhook Rate Limiting Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "URL: $Url" -ForegroundColor White
Write-Host "Total Requests: $TotalRequests" -ForegroundColor White
Write-Host "Expected Limit: 100 requests/minute" -ForegroundColor White
Write-Host ""

# Test payload
$payload = @{
    event = "package:publish"
    name = "primus-test-package"
    version = "1.0.0"
    time = [int](Get-Date -UFormat %s)
    change = @{
        dist = @{
            tarball = "https://registry.npmjs.org/primus-test-package/-/primus-test-package-1.0.0.tgz"
        }
    }
} | ConvertTo-Json -Compress

# Generate HMAC-SHA256 signature
$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($Secret)
$hash = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($payload))
$signature = "sha256=" + [BitConverter]::ToString($hash).Replace("-", "").ToLower()

Write-Host "Sending $TotalRequests requests to test rate limiting..." -ForegroundColor Cyan
Write-Host "Legend: " -NoNewline
Write-Host "." -NoNewline -ForegroundColor Green
Write-Host " = 200 OK | " -NoNewline
Write-Host "X" -NoNewline -ForegroundColor Yellow
Write-Host " = 429 Rate Limited | " -NoNewline
Write-Host "!" -NoNewline -ForegroundColor Red
Write-Host " = Error"
Write-Host ""

$results = @{
    Success = 0
    RateLimited = 0
    Errors = 0
    FirstRateLimitedAt = $null
}

$startTime = Get-Date

for ($i = 1; $i -le $TotalRequests; $i++) {
    try {
        $headers = @{
            "Content-Type" = "application/json"
            "X-Npm-Signature" = $signature
        }
        
        $response = Invoke-WebRequest -Uri $Url -Method Post -Body $payload -Headers $headers -ErrorAction Stop
        
        if ($response.StatusCode -eq 200) {
            $results.Success++
            Write-Host "." -NoNewline -ForegroundColor Green
        }
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 429) {
            $results.RateLimited++
            
            if ($results.FirstRateLimitedAt -eq $null) {
                $results.FirstRateLimitedAt = $i
            }
            
            Write-Host "X" -NoNewline -ForegroundColor Yellow
        }
        else {
            $results.Errors++
            Write-Host "!" -NoNewline -ForegroundColor Red
        }
    }
    
    # Progress update every 50 requests
    if ($i % 50 -eq 0) {
        Write-Host ""
        $elapsed = (Get-Date) - $startTime
        $avgTime = $elapsed.TotalMilliseconds / $i
        Write-Host "Progress: $i/$TotalRequests requests | Avg: $([math]::Round($avgTime, 2))ms/req | " -NoNewline -ForegroundColor Cyan
        Write-Host "Success: $($results.Success) | " -NoNewline -ForegroundColor Green
        Write-Host "Rate Limited: $($results.RateLimited) | " -NoNewline -ForegroundColor Yellow
        Write-Host "Errors: $($results.Errors)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host ""

$totalElapsed = (Get-Date) - $startTime

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Results" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Total Time: $([math]::Round($totalElapsed.TotalSeconds, 2))s" -ForegroundColor White
Write-Host "  Successful (200): $($results.Success)" -ForegroundColor Green
Write-Host "  Rate Limited (429): $($results.RateLimited)" -ForegroundColor Yellow
Write-Host "  Errors: $($results.Errors)" -ForegroundColor Red

if ($results.FirstRateLimitedAt) {
    Write-Host "  First rate limit at request #$($results.FirstRateLimitedAt)" -ForegroundColor Yellow
}

Write-Host ""

# Evaluate test results
$testPassed = $false

if ($results.RateLimited -gt 0) {
    if ($results.FirstRateLimitedAt -le 110 -and $results.FirstRateLimitedAt -ge 95) {
        Write-Host "✅ PASS: Rate limiting is working correctly!" -ForegroundColor Green
        Write-Host "   Rate limit triggered around request 100 (actual: $($results.FirstRateLimitedAt))" -ForegroundColor Green
        $testPassed = $true
    }
    else {
        Write-Host "⚠️  PARTIAL: Rate limiting is active but threshold is off" -ForegroundColor Yellow
        Write-Host "   Expected first rate limit at ~100, got $($results.FirstRateLimitedAt)" -ForegroundColor Yellow
    }
}
else {
    Write-Host "❌ FAIL: No rate limiting detected!" -ForegroundColor Red
    Write-Host "   Expected 429 responses after ~100 requests" -ForegroundColor Red
}

Write-Host ""

if ($results.Errors -gt 0) {
    Write-Host "⚠️  Warning: $($results.Errors) requests failed with errors (not 200 or 429)" -ForegroundColor Yellow
    Write-Host "   This might indicate authentication issues or server errors" -ForegroundColor Yellow
    Write-Host ""
}

# Recommendations
if (-not $testPassed) {
    Write-Host "Troubleshooting:" -ForegroundColor Cyan
    Write-Host "  1. Verify AspNetCoreRateLimit is configured in Program.cs" -ForegroundColor White
    Write-Host "  2. Check appsettings.json for IpRateLimiting section" -ForegroundColor White
    Write-Host "  3. Ensure app.UseIpRateLimiting() is called before app.UseCors()" -ForegroundColor White
    Write-Host "  4. Restart the API server to apply rate limiting changes" -ForegroundColor White
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan

exit ($testPassed ? 0 : 1)

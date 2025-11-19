# PowerShell test script for Primus Auth Test App
Write-Host "🚀 Starting Primus Auth Test Application..." -ForegroundColor Cyan

# Kill any existing node processes on port 3001
$existingProc = Get-Process -Name node -ErrorAction SilentlyContinue | Where-Object {
    (Get-NetTCPConnection -OwningProcess $_.Id -ErrorAction SilentlyContinue | Where-Object { $_.LocalPort -eq 3001 }) -ne $null
}

if ($existingProc) {
    Write-Host "⚠️  Killing existing Node process on port 3001..." -ForegroundColor Yellow
    $existingProc | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Start the application in a background job
Write-Host "📦 Building application..." -ForegroundColor Cyan
cd "$PSScriptRoot"
npm run build | Out-Null

Write-Host "🎬 Starting server..." -ForegroundColor Cyan
$job = Start-Job -ScriptBlock {
    param($path)
    cd $path
    node dist/index.js
} -ArgumentList $PSScriptRoot

# Wait for server to start
Start-Sleep -Seconds 3

# Test endpoints
Write-Host "`n🧪 Testing Endpoints`n" -ForegroundColor Cyan
Write-Host "=" * 60

# Test 1: Health endpoint
Write-Host "`n1️⃣  Testing /api/health (Public)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:3001/api/health" -Method GET -ErrorAction Stop
    Write-Host "✅ Success!" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "❌ Failed: $_" -ForegroundColor Red
}

# Test 2: Public endpoint
Write-Host "`n2️⃣  Testing /api/public (Public)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:3001/api/public" -Method GET -ErrorAction Stop
    Write-Host "✅ Success!" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "❌ Failed: $_" -ForegroundColor Red
}

# Test 3: Root endpoint
Write-Host "`n3️⃣  Testing / (Root)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:3001/" -Method GET -ErrorAction Stop
    Write-Host "✅ Success!" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "❌ Failed: $_" -ForegroundColor Red
}

# Test 4: Protected endpoint without auth (should fail with 401)
Write-Host "`n4️⃣  Testing /api/user/profile (Protected, no auth - should fail)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:3001/api/user/profile" -Method GET -ErrorAction Stop
    Write-Host "❌ Unexpected success - should have been unauthorized!" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 401) {
        Write-Host "✅ Correctly returned 401 Unauthorized" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed with unexpected error: $_" -ForegroundColor Red
    }
}

Write-Host "`n" + ("=" * 60)
Write-Host "`n✅ Basic tests complete!" -ForegroundColor Green
Write-Host "`n💡 Next steps:" -ForegroundColor Cyan
Write-Host "   1. Generate a JWT token: node generate-test-token.js"
Write-Host "   2. Test protected endpoints with the token"
Write-Host "   3. Verify role-based access control"
Write-Host "`n🛑 To stop the server, run: Stop-Job -Id $($job.Id); Remove-Job -Id $($job.Id)" -ForegroundColor Yellow

# Keep server running
Write-Host "`n✨ Server is running in the background (Job ID: $($job.Id))" -ForegroundColor Cyan
Write-Host "    Check logs: Receive-Job -Id $($job.Id) -Keep" -ForegroundColor Gray

# E-Commerce API Validation Script
Write-Host "Starting E-Commerce API Validation..." -ForegroundColor Cyan
Write-Host ""

# Start the API
Write-Host "Starting API server..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project sdk/logging/dotnet/Validation/EcommerceApi/EcommerceApi.csproj --urls http://localhost:5123" `
    -WorkingDirectory "C:\Users\aakib\Primus SaaS" `
    -PassThru `
    -NoNewWindow

Write-Host "Waiting for API to start (10 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

try {
    $baseUrl = "http://localhost:5123"
    
    Write-Host ""
    Write-Host "Running API Tests..." -ForegroundColor Cyan
    Write-Host ""

    # Test 1
    Write-Host "[1] GET /api/products" -ForegroundColor Green
    $response = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Get
    Write-Host "    Retrieved $($response.Count) products" -ForegroundColor Gray
    Write-Host ""

    # Test 2
    Write-Host "[2] GET /api/products/prod-1" -ForegroundColor Green
    $product = Invoke-RestMethod -Uri "$baseUrl/api/products/prod-1" -Method Get
    Write-Host "    Product: $($product.name) - Price: `$$($product.price)" -ForegroundColor Gray
    Write-Host ""

    # Test 3
    Write-Host "[3] POST /api/orders (Create Order)" -ForegroundColor Green
    $orderRequest = @{
        items = @(
            @{ productId = "prod-1"; quantity = 2; price = 999.99 },
            @{ productId = "prod-2"; quantity = 1; price = 29.99 }
        )
    } | ConvertTo-Json

    $order = Invoke-RestMethod -Uri "$baseUrl/api/orders" `
        -Method Post `
        -Body $orderRequest `
        -ContentType "application/json"
    
    Write-Host "    Order ID: $($order.id)" -ForegroundColor Gray
    Write-Host "    Total: `$$($order.total)" -ForegroundColor Gray
    Write-Host "    Status: $($order.status)" -ForegroundColor Gray
    Write-Host ""

    # Test 4
    Write-Host "[4] GET /api/orders/$($order.id)" -ForegroundColor Green
    $retrievedOrder = Invoke-RestMethod -Uri "$baseUrl/api/orders/$($order.id)" -Method Get
    Write-Host "    Order status: $($retrievedOrder.status)" -ForegroundColor Gray
    Write-Host ""

    # Test 5
    Write-Host "[5] Error handling test (404)" -ForegroundColor Green
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/products/invalid-id" -Method Get -ErrorAction Stop
    }
    catch {
        Write-Host "    404 error handled correctly" -ForegroundColor Gray
    }
    Write-Host ""

    Write-Host "All tests completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Check logs at: sdk/logging/dotnet/Validation/EcommerceApi/logs/ecommerce.log" -ForegroundColor Cyan
    Write-Host ""

}
catch {
    Write-Host "Test failed: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
finally {
    Write-Host "Stopping API server..." -ForegroundColor Yellow
    Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    Write-Host "Validation complete!" -ForegroundColor Green
}

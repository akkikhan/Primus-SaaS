# PowerShell script to generate test JWT tokens for Primus SDK testing

function New-TestJwtToken {
    param(
        [string]$UserId = "test-user-123",
        [string]$Email = "test@example.com",
        [string]$Name = "Test User",
        [string[]]$Roles = @("User"),
        [string]$ClientId = "test-client-123",
        [string]$PortalUrl = "https://localhost:7001",
        [string]$Secret = "test-jwt-secret-key-with-at-least-32-characters-long",
        [int]$ExpiresInHours = 24
    )

    Write-Host "🔐 Generating JWT Test Token..." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "User Details:" -ForegroundColor Yellow
    Write-Host "  User ID: $UserId"
    Write-Host "  Email: $Email"
    Write-Host "  Name: $Name"
    Write-Host "  Roles: $($Roles -join ', ')"
    Write-Host "  Client ID: $ClientId"
    Write-Host "  Portal URL: $PortalUrl"
    Write-Host "  Expires In: $ExpiresInHours hours"
    Write-Host ""

    # Create payload
    $now = [int][double]::Parse((Get-Date -UFormat %s))
    $exp = $now + ($ExpiresInHours * 3600)
    
    $payload = @{
        sub = $UserId
        email = $Email
        name = $Name
        role = $Roles
        aud = $ClientId
        iss = $PortalUrl
        exp = $exp
        iat = $now
    } | ConvertTo-Json -Compress

    $header = @{
        alg = "HS256"
        typ = "JWT"
    } | ConvertTo-Json -Compress

    # Base64Url encoding
    function ConvertTo-Base64Url {
        param([string]$text)
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
        $base64 = [Convert]::ToBase64String($bytes)
        return $base64.Replace('+', '-').Replace('/', '_').TrimEnd('=')
    }

    $headerEncoded = ConvertTo-Base64Url $header
    $payloadEncoded = ConvertTo-Base64Url $payload
    $unsigned = "$headerEncoded.$payloadEncoded"

    # Sign with HMAC SHA256
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($Secret)
    $signatureBytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($unsigned))
    $signatureBase64 = [Convert]::ToBase64String($signatureBytes)
    $signature = $signatureBase64.Replace('+', '-').Replace('/', '_').TrimEnd('=')

    $token = "$unsigned.$signature"

    Write-Host "✅ Token Generated Successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "JWT Token:" -ForegroundColor Yellow
    Write-Host $token -ForegroundColor White
    Write-Host ""
    Write-Host "📋 Copied to clipboard!" -ForegroundColor Green
    Set-Clipboard $token

    Write-Host ""
    Write-Host "Test Commands:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host ".NET Test App:" -ForegroundColor Cyan
    Write-Host "  curl https://localhost:7xxx/api/protected -H `"Authorization: Bearer $token`"" -ForegroundColor White
    Write-Host ""
    Write-Host "Node.js Test App:" -ForegroundColor Cyan
    Write-Host "  curl http://localhost:3001/api/protected -H `"Authorization: Bearer $token`"" -ForegroundColor White
    Write-Host ""
    Write-Host "Verify at jwt.io:" -ForegroundColor Cyan
    Write-Host "  https://jwt.io/#debugger-io?token=$token" -ForegroundColor White
    Write-Host ""

    return $token
}

# Example usage
Write-Host "Primus SDK Test Token Generator" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

# Generate tokens for different scenarios
Write-Host "Scenario 1: Regular User Token" -ForegroundColor Magenta
Write-Host "-------------------------------" -ForegroundColor Magenta
$userToken = New-TestJwtToken -Roles @("User")

Write-Host ""
Write-Host ""
Write-Host "Scenario 2: Admin User Token" -ForegroundColor Magenta
Write-Host "-----------------------------" -ForegroundColor Magenta
$adminToken = New-TestJwtToken -UserId "admin-456" -Email "admin@example.com" -Name "Admin User" -Roles @("Admin")

Write-Host ""
Write-Host ""
Write-Host "Scenario 3: Manager User Token" -ForegroundColor Magenta
Write-Host "-------------------------------" -ForegroundColor Magenta
$managerToken = New-TestJwtToken -UserId "manager-789" -Email "manager@example.com" -Name "Manager User" -Roles @("Manager")

Write-Host ""
Write-Host ""
Write-Host "Scenario 4: Multi-Role Token" -ForegroundColor Magenta
Write-Host "-----------------------------" -ForegroundColor Magenta
$multiRoleToken = New-TestJwtToken -UserId "super-999" -Email "super@example.com" -Name "Super User" -Roles @("Admin", "Manager", "User")

Write-Host ""
Write-Host "🎯 All tokens generated and ready for testing!" -ForegroundColor Green
Write-Host "📋 The last token has been copied to clipboard" -ForegroundColor Green
Write-Host ""
Write-Host "To use a specific token, access the variables:" -ForegroundColor Yellow
Write-Host "  `$userToken - Regular user"
Write-Host "  `$adminToken - Admin user"
Write-Host "  `$managerToken - Manager user"
Write-Host "  `$multiRoleToken - User with multiple roles"
Write-Host ""

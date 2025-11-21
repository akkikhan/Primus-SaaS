#!/usr/bin/env pwsh
# Publish PrimusSaaS.Identity.Validator to NuGet.org
# Usage: .\publish-nuget.ps1

$ErrorActionPreference = "Stop"

Write-Host "🚀 Publishing PrimusSaaS.Identity.Validator to NuGet.org" -ForegroundColor Cyan
Write-Host ""

# Check if API key is set
if (-not $env:NUGET_API_KEY) {
    Write-Host "❌ Error: NUGET_API_KEY environment variable not set" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please follow these steps:" -ForegroundColor Yellow
    Write-Host "1. Go to https://www.nuget.org/account/apikeys"
    Write-Host "2. Sign in with your account (or create one if needed)"
    Write-Host "3. Click 'Create' to generate a new API key"
    Write-Host "   - Name: PrimusSaaS.Identity.Validator Publishing"
    Write-Host "   - Select Scopes: Push"
    Write-Host "   - Glob Pattern: PrimusSaaS.Identity.Validator"
    Write-Host "4. Copy the generated API key"
    Write-Host "5. Set the environment variable:"
    Write-Host "   `$env:NUGET_API_KEY = 'your-api-key-here'" -ForegroundColor Green
    Write-Host "6. Run this script again"
    Write-Host ""
    exit 1
}

# Build in Release mode
Write-Host "📦 Building package in Release mode..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}

# Pack the project
Write-Host "📦 Creating NuGet package..." -ForegroundColor Yellow
dotnet pack --configuration Release --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Pack failed" -ForegroundColor Red
    exit 1
}

# Find the package
$package = Get-ChildItem "bin\Release\PrimusSaaS.Identity.Validator.*.nupkg" | Select-Object -First 1
if (-not $package) {
    Write-Host "❌ Package file not found" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Package created: $($package.Name)" -ForegroundColor Green
Write-Host ""

# Confirm before publishing
Write-Host "⚠️  About to publish to NuGet.org:" -ForegroundColor Yellow
Write-Host "   Package: $($package.Name)"
Write-Host "   Source: https://api.nuget.org/v3/index.json"
Write-Host ""
$confirm = Read-Host "Continue? (y/n)"

if ($confirm -ne 'y') {
    Write-Host "❌ Publishing cancelled" -ForegroundColor Yellow
    exit 0
}

# Push to NuGet.org
Write-Host ""
Write-Host "🚀 Publishing to NuGet.org..." -ForegroundColor Yellow
dotnet nuget push "$($package.FullName)" --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Successfully published to NuGet.org!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Next steps:" -ForegroundColor Cyan
    Write-Host "   1. Wait 5-10 minutes for indexing"
    Write-Host "   2. Check package page: https://www.nuget.org/packages/PrimusSaaS.Identity.Validator"
    Write-Host "   3. Test installation: dotnet add package PrimusSaaS.Identity.Validator"
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "❌ Publishing failed" -ForegroundColor Red
    Write-Host "Please check the error message above" -ForegroundColor Yellow
    exit 1
}

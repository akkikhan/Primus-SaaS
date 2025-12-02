<#
.SYNOPSIS
    Primus SaaS Security Vulnerability Scanner
.DESCRIPTION
    Automated security scanning script for the Primus SaaS platform.
    Scans for secrets, vulnerable dependencies, and common security issues.
.EXAMPLE
    .\Run-SecurityScan.ps1
.EXAMPLE
    .\Run-SecurityScan.ps1 -FullScan -OutputReport
#>

param(
    [switch]$FullScan,
    [switch]$OutputReport,
    [switch]$FixIssues,
    [string]$ReportPath = ".\SECURITY_SCAN_RESULTS.md"
)

$ErrorActionPreference = "Continue"
$script:findings = @()
$script:criticalCount = 0
$script:highCount = 0
$script:mediumCount = 0
$script:lowCount = 0

function Add-Finding {
    param(
        [ValidateSet("CRITICAL", "HIGH", "MEDIUM", "LOW")]
        [string]$Severity,
        [string]$Category,
        [string]$Title,
        [string]$FilePath,
        [string]$Description,
        [string]$Remediation
    )
    
    $script:findings += [PSCustomObject]@{
        Severity = $Severity
        Category = $Category
        Title = $Title
        FilePath = $FilePath
        Description = $Description
        Remediation = $Remediation
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    switch ($Severity) {
        "CRITICAL" { $script:criticalCount++; Write-Host "🔴 CRITICAL: $Title" -ForegroundColor Red }
        "HIGH" { $script:highCount++; Write-Host "🟠 HIGH: $Title" -ForegroundColor DarkYellow }
        "MEDIUM" { $script:mediumCount++; Write-Host "🟡 MEDIUM: $Title" -ForegroundColor Yellow }
        "LOW" { $script:lowCount++; Write-Host "🟢 LOW: $Title" -ForegroundColor Green }
    }
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host " $Title" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
}

# ============================================================
# SECTION 1: Secret Detection
# ============================================================
Write-Section "1. Scanning for Hardcoded Secrets"

$secretPatterns = @(
    @{ Pattern = 'password\s*[:=]\s*["\x27][^"\x27]{4,}["\x27]'; Name = "Password" },
    @{ Pattern = 'secret\s*[:=]\s*["\x27][^"\x27]{4,}["\x27]'; Name = "Secret" },
    @{ Pattern = 'api[_-]?key\s*[:=]\s*["\x27][^"\x27]{4,}["\x27]'; Name = "API Key" },
    @{ Pattern = 'clientSecret\s*[:=]\s*["\x27][^"\x27]{4,}["\x27]'; Name = "Client Secret" },
    @{ Pattern = 'connectionString.*Password=[^;]+'; Name = "Connection String Password" },
    @{ Pattern = 'Bearer\s+[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+'; Name = "JWT Token" }
)

$excludePatterns = @(
    "*.md",
    "*.prompt.md",
    "node_modules",
    "bin",
    "obj",
    ".git",
    "package-lock.json"
)

$filesToScan = Get-ChildItem -Path . -Recurse -File -Include "*.json","*.cs","*.ts","*.js","*.yml","*.yaml","*.ps1","*.sh","*.env*" |
    Where-Object { 
        $path = $_.FullName
        -not ($excludePatterns | Where-Object { $path -like "*$_*" })
    }

Write-Host "Scanning $($filesToScan.Count) files for secrets..."

foreach ($file in $filesToScan) {
    try {
        $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
        if ($content) {
            foreach ($pattern in $secretPatterns) {
                if ($content -match $pattern.Pattern) {
                    # Check if it's a placeholder
                    $match = $Matches[0]
                    $isPlaceholder = $match -match 'your-|change-me|<.*>|placeholder|\$\{|example|YOUR_|replace-me'
                    
                    if (-not $isPlaceholder) {
                        Add-Finding -Severity "CRITICAL" `
                            -Category "Secrets" `
                            -Title "Potential $($pattern.Name) found" `
                            -FilePath $file.FullName `
                            -Description "Found pattern matching $($pattern.Name) in file" `
                            -Remediation "Remove the secret and use environment variables or secret management"
                    }
                }
            }
        }
    } catch {
        # Skip files that can't be read
    }
}

# Check for known problematic files
$criticalFiles = @(
    "acme-credentials.json",
    "token.json",
    "token_final.json"
)

foreach ($critFile in $criticalFiles) {
    $found = Get-ChildItem -Path . -Recurse -Name $critFile -ErrorAction SilentlyContinue
    if ($found) {
        Add-Finding -Severity "CRITICAL" `
            -Category "Secrets" `
            -Title "Sensitive file committed: $critFile" `
            -FilePath $found `
            -Description "This file likely contains credentials and should not be in version control" `
            -Remediation "Remove from repo, add to .gitignore, rotate any exposed credentials"
    }
}

# ============================================================
# SECTION 2: NuGet Vulnerability Scan
# ============================================================
Write-Section "2. Scanning NuGet Dependencies"

$csprojFiles = Get-ChildItem -Path . -Recurse -Name "*.csproj" | 
    Where-Object { $_ -notlike "*node_modules*" -and $_ -notlike "*bin*" -and $_ -notlike "*obj*" }

Write-Host "Found $($csprojFiles.Count) .csproj files"

foreach ($csproj in $csprojFiles) {
    $csprojPath = (Get-ChildItem -Path . -Recurse -Filter $csproj | Select-Object -First 1).FullName
    if ($csprojPath) {
        $csprojDir = Split-Path $csprojPath -Parent
        Write-Host "  Checking: $csproj"
        
        try {
            Push-Location $csprojDir
            $vulnOutput = dotnet list package --vulnerable 2>&1
            if ($vulnOutput -match "has the following vulnerable packages") {
                Add-Finding -Severity "HIGH" `
                    -Category "Dependencies" `
                    -Title "Vulnerable NuGet packages in $csproj" `
                    -FilePath $csprojPath `
                    -Description ($vulnOutput | Out-String) `
                    -Remediation "Update vulnerable packages to patched versions"
            }
            Pop-Location
        } catch {
            Write-Host "    Could not scan: $_" -ForegroundColor Yellow
            Pop-Location -ErrorAction SilentlyContinue
        }
    }
}

# ============================================================
# SECTION 3: npm Vulnerability Scan
# ============================================================
Write-Section "3. Scanning npm Dependencies"

$packageJsonFiles = Get-ChildItem -Path . -Recurse -Name "package.json" |
    Where-Object { $_ -notlike "*node_modules*" }

Write-Host "Found $($packageJsonFiles.Count) package.json files"

foreach ($pkgJson in $packageJsonFiles) {
    $pkgPath = (Get-ChildItem -Path . -Recurse -Filter $pkgJson | 
        Where-Object { $_.FullName -notlike "*node_modules*" } | 
        Select-Object -First 1).FullName
    
    if ($pkgPath) {
        $pkgDir = Split-Path $pkgPath -Parent
        Write-Host "  Checking: $pkgJson"
        
        try {
            Push-Location $pkgDir
            if (Test-Path "package-lock.json") {
                $auditOutput = npm audit --json 2>&1 | ConvertFrom-Json -ErrorAction SilentlyContinue
                if ($auditOutput.metadata.vulnerabilities) {
                    $vulns = $auditOutput.metadata.vulnerabilities
                    if ($vulns.critical -gt 0 -or $vulns.high -gt 0) {
                        Add-Finding -Severity $(if ($vulns.critical -gt 0) { "CRITICAL" } else { "HIGH" }) `
                            -Category "Dependencies" `
                            -Title "Vulnerable npm packages: $($vulns.critical) critical, $($vulns.high) high" `
                            -FilePath $pkgPath `
                            -Description "npm audit found vulnerabilities" `
                            -Remediation "Run 'npm audit fix' or manually update packages"
                    }
                }
            }
            Pop-Location
        } catch {
            Write-Host "    Could not scan: $_" -ForegroundColor Yellow
            Pop-Location -ErrorAction SilentlyContinue
        }
    }
}

# ============================================================
# SECTION 4: Check Downloadable Zip Files
# ============================================================
Write-Section "4. Scanning Downloadable Starter Kits"

$zipDir = "docs-site\static\downloads"
if (Test-Path $zipDir) {
    $zipFiles = Get-ChildItem -Path $zipDir -Filter "*.zip"
    Write-Host "Found $($zipFiles.Count) zip files to scan"
    
    $tempDir = ".\temp-security-scan"
    if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    
    foreach ($zip in $zipFiles) {
        Write-Host "  Extracting and scanning: $($zip.Name)"
        $extractPath = Join-Path $tempDir $zip.BaseName
        
        try {
            Expand-Archive -Path $zip.FullName -DestinationPath $extractPath -Force
            
            # Scan extracted contents for secrets
            $extractedFiles = Get-ChildItem -Path $extractPath -Recurse -File
            foreach ($file in $extractedFiles) {
                $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
                if ($content) {
                    foreach ($pattern in $secretPatterns) {
                        if ($content -match $pattern.Pattern) {
                            $match = $Matches[0]
                            $isPlaceholder = $match -match 'your-|change-me|<.*>|placeholder|\$\{|example|YOUR_|replace-me'
                            
                            if (-not $isPlaceholder) {
                                Add-Finding -Severity "CRITICAL" `
                                    -Category "Starter Kits" `
                                    -Title "Secret in downloadable: $($zip.Name)" `
                                    -FilePath "$($zip.Name) -> $($file.Name)" `
                                    -Description "Found $($pattern.Name) in starter kit" `
                                    -Remediation "Remove secret from zip file and regenerate"
                            }
                        }
                    }
                }
            }
        } catch {
            Write-Host "    Could not scan: $_" -ForegroundColor Yellow
        }
    }
    
    # Cleanup
    if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
}

# ============================================================
# SECTION 5: Check .gitignore Coverage
# ============================================================
Write-Section "5. Checking .gitignore Coverage"

$gitignorePath = ".\.gitignore"
if (Test-Path $gitignorePath) {
    $gitignore = Get-Content $gitignorePath -Raw
    
    $requiredPatterns = @(
        ".env",
        ".env.local",
        "*.secret*",
        "*credentials*",
        "appsettings.Development.json",
        "acme-credentials.json",
        "token.json"
    )
    
    foreach ($pattern in $requiredPatterns) {
        if ($gitignore -notmatch [regex]::Escape($pattern)) {
            Add-Finding -Severity "HIGH" `
                -Category "Configuration" `
                -Title "Missing .gitignore pattern: $pattern" `
                -FilePath $gitignorePath `
                -Description "The pattern '$pattern' is not in .gitignore" `
                -Remediation "Add '$pattern' to .gitignore to prevent accidental commits"
        }
    }
} else {
    Add-Finding -Severity "HIGH" `
        -Category "Configuration" `
        -Title "No .gitignore file found" `
        -FilePath "." `
        -Description "Repository has no .gitignore file" `
        -Remediation "Create a .gitignore with appropriate patterns"
}

# ============================================================
# SECTION 6: Check GitHub Workflows
# ============================================================
Write-Section "6. Scanning GitHub Actions Workflows"

$workflowDir = ".\.github\workflows"
if (Test-Path $workflowDir) {
    $workflows = Get-ChildItem -Path $workflowDir -Filter "*.yml"
    
    foreach ($workflow in $workflows) {
        $content = Get-Content $workflow.FullName -Raw
        
        # Check for hardcoded secrets
        if ($content -match '(password|secret|token|key)\s*:\s*[^\$\{]["\x27]?[A-Za-z0-9+/=]{8,}') {
            Add-Finding -Severity "CRITICAL" `
                -Category "CI/CD" `
                -Title "Hardcoded secret in workflow: $($workflow.Name)" `
                -FilePath $workflow.FullName `
                -Description "Found what appears to be a hardcoded secret" `
                -Remediation "Use GitHub Secrets: \${{ secrets.SECRET_NAME }}"
        }
        
        # Check for unpinned actions
        if ($content -match 'uses:\s*\S+@(main|master|latest)') {
            Add-Finding -Severity "MEDIUM" `
                -Category "CI/CD" `
                -Title "Unpinned GitHub Action in: $($workflow.Name)" `
                -FilePath $workflow.FullName `
                -Description "Actions should be pinned to specific versions or SHA" `
                -Remediation "Pin actions to specific versions: uses: actions/checkout@v4.1.1"
        }
    }
}

# ============================================================
# SECTION 7: Check for Debug Endpoints in Production
# ============================================================
Write-Section "7. Checking for Debug/Diagnostic Endpoints"

$csFiles = Get-ChildItem -Path . -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*Test*" -and $_.FullName -notlike "*bin*" -and $_.FullName -notlike "*obj*" }

foreach ($csFile in $csFiles) {
    $content = Get-Content $csFile.FullName -Raw -ErrorAction SilentlyContinue
    if ($content) {
        # Check for diagnostic endpoints without environment checks
        if ($content -match 'MapPrimus(Dev)?Diagnostics|/debug/|/diagnostics/' -and 
            $content -notmatch 'IsDevelopment|Environment\.') {
            Add-Finding -Severity "MEDIUM" `
                -Category "Debug Endpoints" `
                -Title "Diagnostic endpoint may be exposed in production" `
                -FilePath $csFile.FullName `
                -Description "Found diagnostic endpoint without environment check" `
                -Remediation "Wrap diagnostic endpoints in 'if (app.Environment.IsDevelopment())'"
        }
    }
}

# ============================================================
# Generate Report
# ============================================================
Write-Section "Security Scan Complete"

Write-Host ""
Write-Host "Summary:" -ForegroundColor White
Write-Host "  🔴 Critical: $script:criticalCount" -ForegroundColor $(if ($script:criticalCount -gt 0) { "Red" } else { "Green" })
Write-Host "  🟠 High:     $script:highCount" -ForegroundColor $(if ($script:highCount -gt 0) { "DarkYellow" } else { "Green" })
Write-Host "  🟡 Medium:   $script:mediumCount" -ForegroundColor $(if ($script:mediumCount -gt 0) { "Yellow" } else { "Green" })
Write-Host "  🟢 Low:      $script:lowCount" -ForegroundColor Green
Write-Host ""

if ($OutputReport -or $script:criticalCount -gt 0 -or $script:highCount -gt 0) {
    Write-Host "Generating report: $ReportPath" -ForegroundColor Cyan
    
    $report = @"
# Primus SaaS Security Scan Report

**Scan Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Branch:** $(git branch --show-current 2>$null)
**Commit:** $(git rev-parse --short HEAD 2>$null)

## Summary

| Severity | Count |
|----------|-------|
| 🔴 Critical | $script:criticalCount |
| 🟠 High | $script:highCount |
| 🟡 Medium | $script:mediumCount |
| 🟢 Low | $script:lowCount |

## Findings

"@

    foreach ($finding in ($script:findings | Sort-Object @{Expression={
        switch ($_.Severity) {
            "CRITICAL" { 1 }
            "HIGH" { 2 }
            "MEDIUM" { 3 }
            "LOW" { 4 }
        }
    }})) {
        $severityEmoji = switch ($finding.Severity) {
            "CRITICAL" { "🔴" }
            "HIGH" { "🟠" }
            "MEDIUM" { "🟡" }
            "LOW" { "🟢" }
        }
        
        $report += @"

### $severityEmoji [$($finding.Severity)] $($finding.Title)

- **Category:** $($finding.Category)
- **File:** ``$($finding.FilePath)``
- **Description:** $($finding.Description)
- **Remediation:** $($finding.Remediation)

---

"@
    }
    
    $report | Out-File -FilePath $ReportPath -Encoding utf8
    Write-Host "Report saved to: $ReportPath" -ForegroundColor Green
}

if ($script:criticalCount -gt 0) {
    Write-Host ""
    Write-Host "⚠️  CRITICAL VULNERABILITIES FOUND! Review immediately." -ForegroundColor Red
    exit 1
}

exit 0

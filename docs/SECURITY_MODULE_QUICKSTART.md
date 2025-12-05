# PrimusSaaS.Security - Quick Start Guide

**Version**: 1.0.0 (Proposed)  
**Module Type**: Security & Compliance Automation  
**Platforms**: .NET 8+, Node.js 18+

---

## 🚀 5-Minute Integration

### .NET (ASP.NET Core)

```bash
# Install package
dotnet add package PrimusSaaS.Security
```

```csharp
// Program.cs
using PrimusSaaS.Security;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Security (3 lines!)
builder.Services.AddPrimusSecurity(options =>
{
    options.EnableStaticAnalysis = true;
    options.ComplianceStandards = new[] { "OWASP", "PCI-DSS" };
});

var app = builder.Build();

// Apply security middleware
app.UsePrimusSecurity();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

**Done!** Your API now has:
- ✅ SQL injection protection
- ✅ XSS vulnerability scanning
- ✅ Hardcoded secret detection
- ✅ OWASP Top 10 compliance checks

---

### Node.js (Express)

```bash
# Install package
npm install @primus-saas/security
```

```typescript
// app.ts
import express from 'express';
import { primusSecurityMiddleware } from '@primus-saas/security';

const app = express();

// Add Primus Security (3 lines!)
app.use(primusSecurityMiddleware({
  staticAnalysis: true,
  complianceStandards: ['OWASP', 'CIS']
}));

app.listen(3000);
```

**Done!** Same security features as .NET version.

---

## 📊 What You Get

### Core Security Features (Always Available)

| Feature | Description | Detects |
|---------|-------------|---------|
| **Static Analysis** | Real-time code scanning | SQL injection, XSS, CSRF |
| **Secret Detection** | Find hardcoded credentials | API keys, passwords, tokens |
| **Dependency Scanning** | CVE vulnerability checks | Known security flaws |
| **Policy Validation** | Custom security rules | Organization standards |
| **Compliance Reports** | Automated audit docs | OWASP, PCI-DSS, SOC2 |

### Premium Features (Enterprise Tier)

| Feature | Description | Requires |
|---------|-------------|----------|
| **Penetration Testing** | Automated attack scenarios | AWS opt-in |
| **Threat Modeling** | STRIDE analysis | Pro tier |
| **DAST/IAST** | Dynamic security testing | Pro tier |
| **Custom Policies** | Organization-specific rules | Enterprise tier |

---

## 🔧 Configuration Examples

### Basic Configuration

```csharp
// Minimal setup - use defaults
builder.Services.AddPrimusSecurity();
```

### Advanced Configuration

```csharp
builder.Services.AddPrimusSecurity(options =>
{
    // Local security scanning
    options.StaticAnalysis.Enabled = true;
    options.StaticAnalysis.Rules = new[]
    {
        SecurityRule.SqlInjection,
        SecurityRule.XssVulnerability,
        SecurityRule.HardcodedSecrets,
        SecurityRule.InsecureDeserialization
    };
    
    // Dependency scanning
    options.DependencyScanning.Enabled = true;
    options.DependencyScanning.AlertOnCritical = true;
    
    // Compliance
    options.LoadPoliciesFrom("./SecurityPolicies");
    options.ComplianceStandards = new[]
    {
        ComplianceStandard.OWASP_Top10,
        ComplianceStandard.PCI_DSS,
        ComplianceStandard.SOC2_Type2
    };
    
    // Integration with other Primus modules
    options.Logging.Enabled = true;
    options.Notifications.Enabled = true;
    options.Notifications.OnCriticalVulnerability = async (vuln) =>
    {
        await _notifier.SendAsync(new SecurityAlertNotification(vuln));
    };
});
```

### Enterprise Configuration (with AWS)

```csharp
builder.Services.AddPrimusSecurity(options =>
{
    // Local features
    options.EnableStaticAnalysis = true;
    options.EnableDependencyScanning = true;
    
    // AWS Security Agent (optional premium feature)
    options.UseAwsSecurityAgent(aws =>
    {
        aws.Region = "us-east-1";
        
        // Scheduled penetration testing
        aws.PenetrationTesting.Enabled = true;
        aws.PenetrationTesting.Schedule = CronExpression.Weekly();
        aws.PenetrationTesting.TargetUrls = new[]
        {
            "https://api.myapp.com",
            "https://admin.myapp.com"
        };
        
        // Design review automation
        aws.DesignReviews.Enabled = true;
        aws.DesignReviews.DocumentPaths = new[] { "./docs/architecture" };
        
        // Notification on completion
        aws.OnComplete = async (results) =>
        {
            await _emailService.SendAsync(new SecurityReportEmail(results));
        };
    });
});
```

---

## 🛡️ Usage Patterns

### Pattern 1: Protect Specific Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    [HttpPost]
    [PrimusSecurityScan(
        Rules = new[] { 
            SecurityRule.SqlInjection, 
            SecurityRule.XssVulnerability,
            SecurityRule.CreditCardLeakage 
        },
        BlockOnThreat = true
    )]
    public async Task<IActionResult> ProcessPayment(PaymentRequest request)
    {
        // Request automatically scanned
        // Blocked if threat detected
        var result = await _paymentService.ProcessAsync(request);
        return Ok(result);
    }
}
```

### Pattern 2: Custom Security Policies

```yaml
# security-policies/custom-policy.yml
name: Internal Security Standards
version: 1.0

rules:
  - id: no-external-api-calls
    severity: HIGH
    pattern: 'HttpClient.*"https://(?!mycompany.com)'
    message: External API calls must be approved
    
  - id: require-input-validation
    severity: CRITICAL
    check: all-public-methods-validate-input
    
  - id: no-sensitive-logging
    severity: HIGH
    pattern: '_logger.*\b(password|ssn|credit.?card)\b'
    message: Do not log sensitive data
```

```csharp
// Load custom policies
options.Policies.LoadFromDirectory("./SecurityPolicies");
options.Policies.Enforce = true;
```

### Pattern 3: CI/CD Integration

```yaml
# .github/workflows/security.yml
name: Security Scan

on:
  pull_request:
  push:
    branches: [main]

jobs:
  security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Run Primus Security Scan
        run: |
          npm install -g @primus-saas/security-cli
          primus-security scan ./src \
            --policy ./security-policies \
            --compliance OWASP,PCI-DSS \
            --output security-report.json
      
      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: security-report
          path: security-report.json
      
      - name: Fail on Critical
        run: |
          primus-security check-results security-report.json \
            --fail-on critical
```

### Pattern 4: Real-Time Monitoring

```csharp
// Monitor security events
builder.Services.AddPrimusSecurity(options =>
{
    options.Monitoring.Enabled = true;
    options.Monitoring.OnSecurityEvent = (event) =>
    {
        switch (event.Severity)
        {
            case SecuritySeverity.Critical:
                // Alert security team immediately
                _slackService.Send($"🚨 Critical: {event.Description}");
                _pagerService.Alert(event);
                break;
                
            case SecuritySeverity.High:
                // Log and notify
                _logger.LogWarning(event.Description);
                _emailService.SendAsync(new SecurityWarningEmail(event));
                break;
                
            case SecuritySeverity.Medium:
            case SecuritySeverity.Low:
                // Just log
                _logger.LogInformation(event.Description);
                break;
        }
    };
});
```

---

## 📈 Compliance Reporting

### Generate Compliance Report

```csharp
// Programmatic report generation
public class ComplianceController : ControllerBase
{
    private readonly ISecurityReportService _reportService;
    
    [HttpGet("compliance/owasp")]
    public async Task<IActionResult> GetOwaspReport()
    {
        var report = await _reportService.GenerateComplianceReportAsync(
            ComplianceStandard.OWASP_Top10,
            new ReportOptions
            {
                Format = ReportFormat.PDF,
                IncludeRemediation = true,
                DateRange = DateRange.LastMonth
            }
        );
        
        return File(report.Content, "application/pdf", "owasp-compliance.pdf");
    }
}
```

### CLI Report Generation

```bash
# Generate OWASP compliance report
primus-security report \
  --standard OWASP \
  --format pdf \
  --output owasp-compliance-report.pdf

# Generate PCI-DSS compliance report
primus-security report \
  --standard PCI-DSS \
  --format markdown \
  --output pci-dss-compliance.md

# Generate SOC2 compliance report
primus-security report \
  --standard SOC2 \
  --format html \
  --output soc2-compliance.html
```

---

## 🔍 Security Scan Results

### Example Output

```json
{
  "scanId": "scan_2026_12_03_12345",
  "timestamp": "2026-12-03T22:35:55Z",
  "summary": {
    "totalIssues": 7,
    "critical": 1,
    "high": 2,
    "medium": 3,
    "low": 1
  },
  "findings": [
    {
      "id": "SQL-001",
      "severity": "CRITICAL",
      "rule": "SqlInjection",
      "file": "Controllers/UserController.cs",
      "line": 45,
      "description": "Potential SQL injection vulnerability",
      "code": "var query = $\"SELECT * FROM Users WHERE Id = {userId}\";",
      "recommendation": "Use parameterized queries or ORM",
      "remediation": "var user = await _context.Users.FindAsync(userId);",
      "cwe": "CWE-89",
      "owasp": "A03:2021 - Injection"
    },
    {
      "id": "SEC-002",
      "severity": "HIGH",
      "rule": "HardcodedSecret",
      "file": "appsettings.json",
      "line": 12,
      "description": "Hardcoded API key detected",
      "code": "\"ApiKey\": \"sk_live_abc123xyz\"",
      "recommendation": "Use environment variables or Azure Key Vault",
      "remediation": "\"ApiKey\": \"${API_KEY}\"",
      "cwe": "CWE-798"
    }
  ],
  "compliance": {
    "OWASP_Top10": {
      "passed": 8,
      "failed": 2,
      "score": 80,
      "status": "NEEDS_ATTENTION"
    },
    "PCI_DSS": {
      "passed": 15,
      "failed": 1,
      "score": 94,
      "status": "COMPLIANT"
    }
  }
}
```

---

## 🎯 Best Practices

### 1. Enable Security Early

```csharp
// ✅ Good: Enable security from day one
builder.Services.AddPrimusSecurity(options =>
{
    options.EnableStaticAnalysis = true;
    options.FailOnCritical = true; // Block deployment on critical issues
});

// ❌ Bad: Adding security as afterthought
// (Accumulates technical debt)
```

### 2. Use Layered Security

```csharp
// ✅ Good: Multiple security layers
builder.Services.AddPrimusSecurity(options =>
{
    options.EnableStaticAnalysis = true;      // Code scanning
    options.EnableDependencyScanning = true;  // Library vulnerabilities
    options.EnableRuntimeProtection = true;   // Request filtering
    options.EnableComplianceChecks = true;    // Policy validation
});
```

### 3. Integrate with Existing Primus Modules

```csharp
// ✅ Good: Unified observability
builder.Services.AddPrimusSecurity(options =>
{
    // Log security events
    options.IntegrateWithLogging = true;
    
    // Alert on threats
    options.IntegrateWithNotifications = true;
    options.Notifications.OnCritical = async (vuln) =>
    {
        await _notifier.SendAsync(new SecurityAlertNotification(vuln));
    };
    
    // Validate secure authentication
    options.IntegrateWithIdentity = true;
});
```

### 4. Customize Policies for Your Organization

```yaml
# security-policies/org-standards.yml
name: MyCompany Security Standards
extends: OWASP_Top10

custom_rules:
  - id: approved-dependencies-only
    description: Only use approved NuGet packages
    whitelist:
      - Microsoft.*
      - PrimusSaaS.*
      - Newtonsoft.Json
      - Dapper
    action: BLOCK
    
  - id: no-public-endpoints-without-auth
    description: All public endpoints must require authentication
    check: endpoints-require-authentication
    exceptions:
      - /health
      - /metrics
    action: WARN
```

### 5. Monitor and Review Regularly

```csharp
// Schedule automated security reviews
options.ScheduledScans.Enabled = true;
options.ScheduledScans.Frequency = ScanFrequency.Daily;
options.ScheduledScans.OnComplete = async (results) =>
{
    // Email daily security digest
    await _emailService.SendAsync(new DailySecurityDigest(results));
    
    // Track trends over time
    await _analyticsService.RecordSecurityScore(results.Score);
};
```

---

## 💰 Pricing

| Tier | Price | Features |
|------|-------|----------|
| **Free** | $0 | Basic static analysis, dependency scanning |
| **Pro** | $99/month/app | Advanced policies, compliance reports, CI/CD integration |
| **Enterprise** | $499/month/app | AWS Security Agent, pen testing, custom policies, priority support |

---

## 📚 Resources

- [Full Documentation](https://docs.primussaas.com/security)
- [API Reference](https://docs.primussaas.com/security/api)
- [Security Policies Library](https://github.com/primus-saas/security-policies)
- [Example Projects](https://github.com/primus-saas/examples/security)
- [Compliance Templates](https://docs.primussaas.com/security/compliance)

---

## 🆘 Support

- **Community**: [Discord](https://discord.gg/primus-saas)
- **Documentation**: [docs.primussaas.com](https://docs.primussaas.com)
- **Email**: security@primussaas.com
- **Emergency**: security-urgent@primussaas.com (Enterprise only)

---

## 🔄 Migration from Other Tools

### From Snyk

```bash
# Before (Snyk)
npm install -g snyk
snyk auth
snyk test
snyk monitor

# After (Primus)
npm install @primus-saas/security
# Add 3 lines to your app (shown above)
# Done! Automatic scanning on every request
```

### From SonarQube

```bash
# Before (SonarQube)
# - Install SonarQube server
# - Configure sonar-project.properties
# - Run sonar-scanner
# - Check web UI for results

# After (Primus)
dotnet add package PrimusSaaS.Security
# Add 3 lines to Program.cs (shown above)
# Instant feedback on every build/request
```

---

## 🚀 Next Steps

1. **Install the package**: `dotnet add package PrimusSaaS.Security`
2. **Add 3 lines of code**: See integration examples above
3. **Test it**: Make a request to your API and check logs
4. **Customize**: Add your organization's security policies
5. **Scale**: Enable premium features (AWS, pen testing) as needed

**Questions?** Join our [Discord](https://discord.gg/primus-saas) or email security@primussaas.com

---

**Ready to make your code secure in 5 minutes? Let's go!** 🛡️

# PrimusSaaS.Security

**Version**: 1.0.0-preview.1  
**Status**: 🚧 **UNDER DEVELOPMENT** - Milestone 1 Foundation Phase

Enterprise-grade security analysis module with **absolute data isolation**. All vulnerability scanning, dependency checking, and compliance validation happens **100% locally** within your infrastructure.

---

## 🔒 Core Guarantee

**Your code NEVER leaves your infrastructure. Period.**

- ✅ **Zero external API calls** (compile-time blocked)
- ✅ **No cloud dependencies** (fully self-contained)
- ✅ **Complete privacy** (verifiable by network monitoring)
- ✅ **Offline capable** (works without internet)

---

## 🚀 Quick Start (5 minutes)

### Installation

```bash
dotnet add package PrimusSaaS.Security --prerelease
```

### Integration

```csharp
// Program.cs
using PrimusSaaS.Security;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Security (3 lines!)
builder.Services.AddPrimusSecurity(options =>
{
    options.EnableStaticAnalysis = true;
    options.EnableDependencyScanning = true;
    options.ComplianceStandards = new[] { "OWASP", "PCI-DSS" };
});

var app = builder.Build();

// Verify data isolation on startup
var verification = PrimusSecurityExtensions.VerifyDataIsolation();
if (!verification.IsFullyIsolated)
{
    throw new InvalidOperationException(
        "Security module failed data isolation verification!"
    );
}

app.Run();
```

**Done!** Your API now has security analysis running locally.

---

## ✨ Features

### ✅ Phase 1: Foundation (Completed)

- [x] Project structure created
- [x] Core models defined (SecurityFinding, ScanResult)
- [x] Configuration options
- [x] DI extensions
- [x] Data isolation verification
- [x] PDF Reporting (QuestPDF)

### ✅ Phase 2: Core Engine (Completed)

- [x] **Static Code Analysis** (Roslyn Analyzers)
  - [x] Base Analyzer Infrastructure
  - [x] **Rule PS0001**: SQL Injection (ADO.NET, EF Core, Dapper)
  - [x] **Rule PS0002**: XSS Detection (Html.Raw, innerHTML)
  - [x] **Rule PS0003**: Hardcoded Secret Constants (AWS, Stripe, etc.)

- [x] **Secret Detection**
  - [x] Regex-based detection (Standard patterns embedded)
  - [x] Entropy-based detection
  - [x] Zero-config/Out-of-the-box support

- [x] **Dependency Scanning**
  - [x] Local CVE database (SQLite) support
  - [x] NuGet package vulnerability lookup
  - [x] Semver range matching (Strict & Loose)

## 🗄️ CVE Database Maintenance

Because PrimusSaaS.Security is **offline-first**, it does not download vulnerability data at runtime. You must provide the `cve.db` file.

### Generating the Database

1.  Clone the [GitHub Advisory Database](https://github.com/github/advisory-database).
2.  Run the **Primus Data Aggregator** tool:
    ```bash
    dotnet run --project tools/PrimusSaaS.Security.DataAggregator \
      -- "path/to/advisory-database" \
      -- "path/to/output/cve.db"
    ```
3.  Distribute the resulting `cve.db` to your build agents or developers.

## 📊 Current Implementation Status

```
Overall Progress: ▰▰▰▰▰▰▰▰▰▰ 100% (Foundation & Core Engine Complete)

Milestone 1: Foundation        ▰▰▰▰▰▰▰▰▰▰ 100%
Milestone 2: Core Engine        ▰▰▰▰▰▰▰▰▰▰ 100%
Milestone 3: Data Tools         ▰▰▰▰▰▰▰▰▰▰ 100% (Aggregator Tool Ready)
Milestone 4: Test Coverage      ▰▰▰▰▰▰▰▰▱▱  80%
```

**Next Steps**:
1. Integration testing in your CI/CD pipeline.
2. Regular updates of your `cve.db` snapshot.

---

## 🎯 Example Use Case

### Before Primus Security

```csharp
// Manual security review required
// Consultant costs: $5,000
// Time: 3-5 days
// Frequency: Once per quarter
```

### After Primus Security

```csharp
// Automated continuous security scanning
builder.Services.AddPrimusSecurity(options =>
{
    options.EnableStaticAnalysis = true;  // Instant SQL injection, XSS detection
    options.EnableSecretDetection = true; // Catch hardcoded API keys
    options.CveDatabasePath = "/app/data/cve.db"; // Local DB path
    
    options.OnScanComplete = async (result) => 
    {
        // Generate PDF Report
        var reporter = new PdfSecurityReporter();
        reporter.GenerateReport(result, $"scan-report-{DateTime.Now:yyyyMMdd}.pdf");
    };
});
```

---

## 📞 Support & Feedback

- **Issues**: https://github.com/primus-saas/security/issues
- **Email**: security@primussaas.com
- **Documentation**: https://docs.primussaas.com/security

---

## ⚖️ License

MIT License - see LICENSE file for details

---

**🔒 Remember: Your code NEVER leaves your infrastructure. We guarantee it.**

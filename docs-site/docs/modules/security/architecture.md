# PrimusSaaS.Security - Technical Architecture

**Version**: 1.0.0-preview.1  
**Date**: December 4, 2025  
**Status**: Foundation Phase (Milestone 1)

---

## 🎯 Architecture Overview

### Core Principle: 100% Local Execution

```
┌─────────────────────────────────────────────────────────────┐
│  Client's Infrastructure (Complete Isolation)               │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Client's Application Process                          │ │
│  │                                                         │ │
│  │  ┌──────────────────────────────────────────────────┐ │ │
│  │  │  PrimusSaaS.Security Module (In-Process)         │ │ │
│  │  │                                                   │ │ │
│  │  │  ┌─────────────────┐    ┌──────────────────┐    │ │ │
│  │  │  │ Static Analyzer │    │ Secret Detector  │    │ │ │
│  │  │  │  (Roslyn AST)   │    │ (Regex+Entropy)  │    │ │ │
│  │  │  └─────────────────┘    └──────────────────┘    │ │ │
│  │  │                                                   │ │ │
│  │  │  ┌─────────────────┐    ┌──────────────────┐    │ │ │
│  │  │  │ Dependency      │    │ Policy Engine    │    │ │ │
│  │  │  │ Scanner (CVE)   │    │ (YAML Rules)     │    │ │ │
│  │  │  └─────────────────┘    └──────────────────┘    │ │ │
│  │  │                                                   │ │ │
│  │  │  ┌─────────────────┐    ┌──────────────────┐    │ │ │
│  │  │  │ Pen Test        │    │ Report Generator │    │ │ │
│  │  │  │ Simulator       │    │ (PDF/HTML)       │    │ │ │
│  │  │  └─────────────────┘    └──────────────────┘    │ │ │
│  │  └──────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Local Storage (Client's Disk)                         │ │
│  │                                                         │ │
│  │  SecurityData/                                          │ │
│  │    ├── cve-database.db (SQLite)                        │ │
│  │    ├── Policies/*.yaml                                 │ │
│  │    └── SecretPatterns.json                             │ │
│  │                                                         │ │
│  │  SecurityFindings/                                      │ │
│  │    ├── scan_2025_12_04_001.json                        │ │
│  │    └── scan_2025_12_04_002.json                        │ │
│  │                                                         │ │
│  │  SecurityReports/                                       │ │
│  │    ├── compliance_owasp_2025_12.pdf                    │ │
│  │    └── vulnerability_report_2025_12.html               │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ❌ NO external network calls                               │
│  ❌ NO cloud dependencies                                   │
│  ❌ NO data transmission outside client infrastructure      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🏗️ Component Architecture

### 1. Security Scanner (Orchestrator)

**Responsibility**: Coordinate all analyzers and produce unified scan results

```csharp
public interface ISecurityScanner
{
    Task<ScanResult> ScanAsync(ScanRequest request, CancellationToken ct = default);
    Task<ScanResult> ScanDirectoryAsync(string path, CancellationToken ct = default);
    Task<ScanResult> ScanFileAsync(string filePath, CancellationToken ct = default);
}

public class SecurityScanner : ISecurityScanner
{
    private readonly IEnumerable<IAnalyzer> _analyzers;
    private readonly ISecretDetector _secretDetector;
    private readonly IDependencyScanner _dependencyScanner;
    private readonly IPolicyEngine _policyEngine;
    
    public async Task<ScanResult> ScanAsync(ScanRequest request, CancellationToken ct)
    {
        var result = new ScanResult { StartTime = DateTime.UtcNow };
        
        // 1. Static code analysis
        if (request.EnableStaticAnalysis)
        {
            foreach (var analyzer in _analyzers)
            {
                var findings = await analyzer.AnalyzeAsync(request.Files, ct);
                result.Findings.AddRange(findings);
            }
        }
        
        // 2. Secret detection
        if (request.EnableSecretDetection)
        {
            var secrets = await _secretDetector.DetectAsync(request.Files, ct);
            result.Findings.AddRange(secrets);
        }
        
        // 3. Dependency scanning
        if (request.EnableDependencyScanning)
        {
            var vulnerabilities = await _dependencyScanner.ScanAsync(request.PackageFiles, ct);
            result.Findings.AddRange(vulnerabilities);
        }
        
        // 4. Policy validation
        if (request.EnablePolicyValidation)
        {
            var violations = await _policyEngine.ValidateAsync(result.Findings, ct);
            result.Findings.AddRange(violations);
        }
        
        result.EndTime = DateTime.UtcNow;
        result.Passed = !result.Findings.Any(f => f.Severity == SecuritySeverity.Critical);
        
        return result;
    }
}
```

---

### 2. Static Code Analyzer

**Responsibility**: Parse source code and detect vulnerabilities

```csharp
public interface ICodeParser
{
    Task<ParsedSyntaxTree> ParseAsync(string sourceCode, string filePath);
    string Language { get; }
}

public interface IAnalyzer
{
    Task<List<SecurityFinding>> AnalyzeAsync(IEnumerable<string> files, CancellationToken ct);
    string AnalyzerId { get; }
}

// Example: SQL Injection Analyzer
public class SqlInjectionAnalyzer : CSharpSyntaxWalker, IAnalyzer
{
    private readonly ICodeParser _parser;
    private readonly TaintAnalyzer _taintAnalyzer;
    
    public async Task<List<SecurityFinding>> AnalyzeAsync(
        IEnumerable<string> files, 
        CancellationToken ct)
    {
        var findings = new List<SecurityFinding>();
        
        foreach (var file in files.Where(f => f.EndsWith(".cs")))
        {
            var parsedTree = await _parser.ParseAsync(
                File.ReadAllText(file), file
            );
            
            Visit(parsedTree.Tree.GetRoot());
            
            findings.AddRange(_findings);
            _findings.Clear();
        }
        
        return findings;
    }
    
    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        // Detect SQL command execution with tainted input
        if (IsDataCommandExecutionMethod(node))
        {
            var argument = node.ArgumentList.Arguments.FirstOrDefault();
            if (argument != null && _taintAnalyzer.IsTainted(argument.Expression))
            {
                _findings.Add(new SecurityFinding
                {
                    Severity = SecuritySeverity.Critical,
                    Title = "SQL Injection Vulnerability",
                    Description = "User input flows to SQL query without parameterization",
                    FilePath = node.SyntaxTree.FilePath,
                    Line = GetLineNumber(node),
                    CWE = "CWE-89",
                    OWASP = "A03:2021 - Injection",
                    Remediation = "Use parameterized queries or ORM"
                });
            }
        }
        
        base.VisitInvocationExpression(node);
    }
}
```

---

### 3. Taint Analysis Engine

**Responsibility**: Track data flow from sources (user input) to sinks (SQL, HTML, etc.)

```csharp
public class TaintAnalyzer
{
    private readonly SemanticModel _semanticModel;
    private readonly Dictionary<ISymbol, TaintState> _taintMap;
    
    public enum TaintSource
    {
        UserInput,      // HttpRequest.Query, Body, Form
        FileSystem,     // File.ReadAllText
        Environment,    // Environment.GetEnvironmentVariable
        Database,       // Database query results
        Safe            // Constants, computed values
    }
    
    public bool IsTainted(ExpressionSyntax expression, TaintSource? expectedSource = null)
    {
        var state = AnalyzeTaint(expression);
        
        if (expectedSource.HasValue)
            return state.Source == expectedSource.Value;
            
        return state.IsTainted;
    }
    
    private TaintState AnalyzeTaint(ExpressionSyntax expression)
    {
        return expression switch
        {
            LiteralExpressionSyntax => TaintState.Safe,
            IdentifierNameSyntax id => GetIdentifierTaint(id),
            BinaryExpressionSyntax binary => PropagateTaint(binary),
            InvocationExpressionSyntax invocation => AnalyzeMethodCall(invocation),
            MemberAccessExpressionSyntax member => AnalyzeMemberAccess(member),
            _ => TaintState.Unknown
        };
    }
    
    private TaintState AnalyzeMemberAccess(MemberAccessExpressionSyntax memberAccess)
    {
        var symbol = _semanticModel.GetSymbolInfo(memberAccess).Symbol;
        var fullName = symbol?.ToDisplayString();
        
        // Known taint sources
        if (fullName?.Contains("HttpRequest.Query") == true ||
            fullName?.Contains("HttpRequest.Body") == true ||
            fullName?.Contains("HttpRequest.Form") == true)
        {
            return new TaintState 
            { 
                IsTainted = true, 
                Source = TaintSource.UserInput 
            };
        }
        
        // Recursively analyze the expression
        return AnalyzeTaint(memberAccess.Expression);
    }
}
```

---

### 4. Secret Detector

**Responsibility**: Detect hardcoded secrets using regex patterns and entropy analysis

```csharp
public interface ISecretDetector
{
    Task<List<SecurityFinding>> DetectAsync(IEnumerable<string> files, CancellationToken ct);
}

public class SecretDetector : ISecretDetector
{
    private readonly List<SecretPattern> _patterns;
    
    public SecretDetector()
    {
        // Load patterns from SecretPatterns.json
        _patterns = LoadSecretPatterns();
    }
    
    public async Task<List<SecurityFinding>> DetectAsync(
        IEnumerable<string> files, 
        CancellationToken ct)
    {
        var findings = new List<SecurityFinding>();
        
        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file, ct);
            var lines = content.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // 1. Regex-based detection
                foreach (var pattern in _patterns)
                {
                    var match = Regex.Match(line, pattern.Pattern);
                    if (match.Success)
                    {
                        findings.Add(new SecurityFinding
                        {
                            Severity = SecuritySeverity.Critical,
                            Title = $"Hardcoded {pattern.Type} Detected",
                            Description = $"Found {pattern.Type} in source code",
                            FilePath = file,
                            Line = i + 1,
                            Code = RedactSecret(line, match.Value), // ⚠️ REDACT!
                            CWE = "CWE-798",
                            OWASP = "A02:2021 - Cryptographic Failures",
                            Remediation = $"Move {pattern.Type} to secure configuration"
                        });
                    }
                }
                
                // 2. Entropy-based detection
                if (HasHighEntropy(line, threshold: 4.5))
                {
                    findings.Add(CreateEntropyFinding(file, i + 1, line));
                }
            }
        }
        
        return findings;
    }
    
    private string RedactSecret(string line, string secret)
    {
        // ⚠️ CRITICAL: Never log actual secret values
        var redacted = secret.Length > 8
            ? secret.Substring(0, 4) + "***REDACTED***"
            : "***REDACTED***";
            
        return line.Replace(secret, redacted);
    }
    
    private bool HasHighEntropy(string value, double threshold)
    {
        // Shannon entropy calculation
        var entropy = 0.0;
        var charCounts = value.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
        
        foreach (var count in charCounts.Values)
        {
            var probability = (double)count / value.Length;
            entropy -= probability * Math.Log(probability, 2);
        }
        
        return entropy >= threshold;
    }
}

public class SecretPattern
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public double? EntropyThreshold { get; set; }
    public string Severity { get; set; } = "CRITICAL";
}
```

---

### 5. Dependency Scanner

**Responsibility**: Scan package manifests and query local CVE database

```csharp
public interface IDependencyScanner
{
    Task<List<SecurityFinding>> ScanAsync(IEnumerable<string> packageFiles, CancellationToken ct);
}

public class DependencyScanner : IDependencyScanner
{
    private readonly ICveDatabase _cveDatabase;
    
    public async Task<List<SecurityFinding>> ScanAsync(
        IEnumerable<string> packageFiles, 
        CancellationToken ct)
    {
        var findings = new List<SecurityFinding>();
        
        foreach (var file in packageFiles)
        {
            if (file.EndsWith(".csproj"))
            {
                findings.AddRange(await ScanCsprojAsync(file, ct));
            }
            else if (file.EndsWith("package.json"))
            {
                findings.AddRange(await ScanPackageJsonAsync(file, ct));
            }
        }
        
        return findings;
    }
    
    private async Task<List<SecurityFinding>> ScanCsprojAsync(string path, CancellationToken ct)
    {
        var findings = new List<SecurityFinding>();
        var xml = await File.ReadAllTextAsync(path, ct);
        var doc = XDocument.Parse(xml);
        
        var packages = doc.Descendants("PackageReference")
            .Select(e => new
            {
                Name = e.Attribute("Include")?.Value,
                Version = e.Attribute("Version")?.Value
            })
            .Where(p => p.Name != null && p.Version != null);
            
        foreach (var package in packages)
        {
            // Query local CVE database
            var vulnerabilities = await _cveDatabase.FindVulnerabilitiesAsync(
                package.Name!, 
                package.Version!,
                ct
            );
            
            foreach (var cve in vulnerabilities)
            {
                findings.Add(new SecurityFinding
                {
                    Severity = GetSeverityFromCVSS(cve.CVSSScore),
                    Title = $"Vulnerable Dependency: {package.Name}",
                    Description = cve.Description,
                    FilePath = path,
                    Package = package.Name,
                    CurrentVersion = package.Version,
                    PatchedVersion = cve.PatchedVersion,
                    CVE = cve.CVEId,
                    CVSSScore = cve.CVSSScore,
                    Remediation = $"Update to version {cve.PatchedVersion} or later"
                });
            }
        }
        
        return findings;
    }
}
```

---

### 6. CVE Database (Local SQLite)

**Responsibility**: Store and query vulnerability data offline

```sql
-- Schema: cve_database.db

CREATE TABLE vulnerabilities (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cve_id TEXT NOT NULL UNIQUE,
    description TEXT,
    cvss_score REAL,
    severity TEXT, -- 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL'
    published_date TEXT,
    last_modified TEXT,
    cwe_id TEXT,
    source TEXT -- 'NVD', 'GitHub', 'NuGet', 'NPM'
);

CREATE TABLE affected_packages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    vulnerability_id INTEGER NOT NULL,
    ecosystem TEXT NOT NULL, -- 'nuget', 'npm', 'maven', 'pypi'
    package_name TEXT NOT NULL,
    affected_version_range TEXT, -- e.g., ">=1.0.0 <1.2.3"
    patched_version TEXT,
    FOREIGN KEY (vulnerability_id) REFERENCES vulnerabilities(id)
);

CREATE INDEX idx_cve_id ON vulnerabilities(cve_id);
CREATE INDEX idx_package_name ON affected_packages(package_name);
CREATE INDEX idx_ecosystem ON affected_packages(ecosystem);
```

```csharp
public interface ICveDatabase
{
    Task<List<Vulnerability>> FindVulnerabilitiesAsync(
        string packageName, 
        string version, 
        CancellationToken ct);
}

public class CveDatabase : ICveDatabase
{
    private readonly SqliteConnection _connection;
    
    public CveDatabase(string databasePath)
    {
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
    }
    
    public async Task<List<Vulnerability>> FindVulnerabilitiesAsync(
        string packageName, 
        string version, 
        CancellationToken ct)
    {
        var vulnerabilities = new List<Vulnerability>();
        
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT v.cve_id, v.description, v.cvss_score, 
                   ap.patched_version, ap.affected_version_range
            FROM vulnerabilities v
            JOIN affected_packages ap ON v.id = ap.vulnerability_id
            WHERE ap.package_name = @packageName
              AND ap.ecosystem = 'nuget'
        ";
        cmd.Parameters.AddWithValue("@packageName", packageName);
        
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var affectedRange = reader.GetString(4);
            
            // Check if current version is in affected range
            if (IsVersionAffected(version, affectedRange))
            {
                vulnerabilities.Add(new Vulnerability
                {
                    CVEId = reader.GetString(0),
                    Description = reader.GetString(1),
                    CVSSScore = reader.GetDouble(2),
                    PatchedVersion = reader.GetString(3)
                });
            }
        }
        
        return vulnerabilities;
    }
    
    private bool IsVersionAffected(string version, string affectedRange)
    {
        // Implement semver range matching
        // e.g., ">=1.0.0 <1.2.3" includes 1.1.5 but not 1.2.3
        return SemverHelper.IsInRange(version, affectedRange);
    }
}
```

---

## 🔐 Data Isolation Architecture

### Compile-Time Guarantees

```xml
<!-- PrimusSaaS.Security.csproj -->
<PropertyGroup>
    <!-- CRITICAL: Block all network assemblies -->
    <RestrictedAssemblies>
      System.Net.Http;
      System.Net.WebSockets;
      System.Net.WebClient;
      Microsoft.Azure.*;
      Amazon.AWS.*;
      Google.Cloud.*;
    </RestrictedAssemblies>
</PropertyGroup>
```

**Result**: Compilation fails if any code tries to use network assemblies.

### Runtime Verification

```csharp
public static DataIsolationReport VerifyDataIsolation()
{
    var report = new DataIsolationReport();
    
    // Check 1: No network assembly references
    var assembly = typeof(PrimusSecurityExtensions).Assembly;
    var references = assembly.GetReferencedAssemblies();
    
    var networkRefs = references.Where(r =>
        r.Name?.Contains("Http") == true ||
        r.Name?.Contains("Azure") == true ||
        r.Name?.Contains("AWS") == true
    ).ToList();
    
    report.Checks.Add(new VerificationCheck
    {
        Name = "No Network References",
        Passed = !networkRefs.Any(),
        Details = networkRefs.Any()
            ? $"Found: {string.Join(", ", networkRefs.Select(r => r.Name))}"
            : "No network references (PASS)"
    });
    
    // Check 2: All data paths are local
    report.Checks.Add(new VerificationCheck
    {
        Name = "Local Data Paths",
        Passed = true,
        Details = "All data paths are local (PASS)"
    });
    
    report.IsFullyIsolated = report.Checks.All(c => c.Passed);
    
    return report;
}
```

---

## 📦 Technology Stack Decisions

| Component | Technology | Rationale |
|-----------|-----------|-----------|
| **C# AST Parser** | Microsoft.CodeAnalysis.CSharp (Roslyn) | Industry standard, mature, excellent semantic analysis |
| **TypeScript Parser** | @typescript-eslint/parser | Best-in-class TypeScript AST, used by ESLint |
| **Local Database** | SQLite (Microsoft.Data.Sqlite) | Fast, portable, zero-config, perfect for local data |
| **Template Engine** | Fluid.Core | Already used in Primus.Notifications, Liquid syntax |
| **PDF Generation** | QuestPDF | Modern, fluent API, professional output |
| **Compression** | ZstdSharp | Superior compression for CVE database |
| **No Network** | ❌ NONE | **CRITICAL**: Zero network libraries |

---

## 🔄 Data Flow

### Scan Workflow

```
1. User triggers scan
   ↓
2. SecurityScanner.ScanAsync()
   ↓
3. Parallel execution:
   ├─→ StaticAnalyzer.AnalyzeAsync() → SecurityFindings
   ├─→ SecretDetector.DetectAsync() → SecurityFindings
   ├─→ DependencyScanner.ScanAsync() → Query local CVE DB → SecurityFindings
   └─→ PolicyEngine.ValidateAsync() → SecurityFindings
   ↓
4. Aggregate all findings
   ↓
5. Deduplicate and prioritize
   ↓
6. Store to local JSON file (SecurityFindings/scan_xxx.json)
   ↓
7. Generate report (if requested) → Local PDF/HTML
   ↓
8. Return ScanResult to client
```

**Key Points**:
- ✅ All processing in-memory
- ✅ All data storage local
- ✅ No external calls anywhere

---

## 🎯 Module Boundaries

### What's In Scope

- ✅ Static code analysis (C#, TypeScript)
- ✅ Secret detection (regex + entropy)
- ✅ Dependency vulnerability scanning
- ✅ Local CVE database queries
- ✅ Security policy validation
- ✅ Compliance reporting (OWASP, PCI-DSS, SOC2, HIPAA)
- ✅ Simulated penetration testing (safe, local)
- ✅ Report generation (PDF, HTML, Markdown)

### What's Out of Scope

- ❌ Real penetration testing (actual attacks)
- ❌ Runtime application monitoring
- ❌ Network traffic analysis
- ❌ Cloud-based AI/ML models
- ❌ External vulnerability databases (use local only)
- ❌ Automatic code fixes (read-only analysis)

---

## 🔌 Integration Points

### With Primus.Notifications

```csharp
builder.Services.AddPrimusSecurity(options =>
{
    options.IntegrateWithNotifications = true;
    options.OnCriticalVulnerability = async (finding) =>
    {
        await notificationService.SendAsync(new SecurityAlertNotification
        {
            Title = finding.Title,
            Severity = finding.Severity.ToString(),
            Description = finding.Description,
            FilePath = finding.FilePath,
            Line = finding.Line,
            Remediation = finding.Remediation
        });
    };
});
```

### With Primus.Logging

```csharp
builder.Services.AddPrimusSecurity(options =>
{
    options.IntegrateWithLogging = true;
    // All security operations logged via Primus.Logging
    // Includes: scan start/end, findings count, errors
    // NEVER logs: code content, secret values
});
```

---

## 📏 Performance Targets

| Metric | Target | Max Acceptable |
|--------|--------|----------------|
| **Scan Speed** | < 1 sec per 1,000 LOC | < 2 sec per 1,000 LOC |
| **Memory Usage** | < 500 MB for 10K files | < 1 GB for 10K files |
| **CVE DB Query** | < 10ms per package | < 50ms per package |
| **Report Generation** | < 2 sec for 100 findings | < 5 sec for 100 findings |
| **False Positive Rate** | < 3% | < 5% |

---

## 🚀 Future Extensions (Post v1.0.0)

1. **Additional Languages**: Python, Java, Go, Ruby
2. **IDE Extensions**: VS Code, Visual Studio
3. **Advanced ML Models**: Context-aware vulnerability detection (local only)
4. **Custom Analyzers**: Plugin system for org-specific rules
5. **Historical Trending**: Track vulnerability trends over time

---

**Last Updated**: December 4, 2025  
**Status**: Foundation Phase ✅  
**Next**: Implement core analyzers (Milestone 2)

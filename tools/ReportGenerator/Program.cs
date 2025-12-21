using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PrimusSaaS.Security.Core;
using PrimusSaaS.Security.Data;
using PrimusSaaS.Security.Detectors;
using PrimusSaaS.Security.Reporting;
using PrimusSaaS.Security.Scanners;

namespace PrimusSaaS.Security.ScannerCLI;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 0. Configuration
        var targetPath = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "examples"));
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "cve.db");
        var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "EndToEndSecurityReport.pdf");

        Console.WriteLine("PrimusSaaS Security Scanner (End-to-End Verification)");
        Console.WriteLine("=====================================================");
        Console.WriteLine($"Target: {targetPath}");
        Console.WriteLine($"Database: {dbPath}");
        Console.WriteLine($"Report Header: {reportPath}");

        var findings = new List<SecurityFinding>();
        var logger = NullLogger<DependencyScanner>.Instance;
        var start = DateTime.UtcNow;

        // 1. Dependency Scanning
        Console.WriteLine("\n[1/3] Running Dependency Scanner...");
        if (File.Exists(dbPath))
        {
            var depScanner = new DependencyScanner(logger, dbPath);
            var depFindings = await depScanner.ScanAsync(targetPath);
            findings.AddRange(depFindings);
            Console.WriteLine($" -> Found {depFindings.Count()} dependency issues.");
        }
        else
        {
            Console.WriteLine(" -> WARNING: cve.db not found. Skipping dependency scan.");
        }

        // 2. Secret Scanning
        Console.WriteLine("\n[2/3] Running Secret Scanner...");
        var secretLogger = NullLogger<SecretDetector>.Instance;
        var patternLogger = NullLogger<FileSecretPatternProvider>.Instance;
        var patternProvider = new FileSecretPatternProvider(patternLogger, Path.Combine(AppContext.BaseDirectory, "Data/SecretPatterns.json")); // Built-in patterns
        
        // Fallback if the JSON isn't copied to bin relative to this execution
        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "Data/SecretPatterns.json")))
        {
             // Try to find it in the source tree if running from root
             var srcPath = Path.Combine(Directory.GetCurrentDirectory(), "sdk/dotnet/PrimusSaaS.Security/Data/SecretPatterns.json");
             if(File.Exists(srcPath)) patternProvider = new FileSecretPatternProvider(patternLogger, srcPath);
        }

        var secretDetector = new SecretDetector(secretLogger, patternProvider);
        
        var sourceFiles = Directory.GetFiles(targetPath, "*.cs", SearchOption.AllDirectories)
             .Concat(Directory.GetFiles(targetPath, "*.json", SearchOption.AllDirectories)) // Check config files too
             .Concat(Directory.GetFiles(targetPath, "*.config", SearchOption.AllDirectories));

        int filesScanned = 0;
        foreach (var file in sourceFiles)
        {
            if (file.Contains("obj") || file.Contains("bin")) continue; // Skip build artifacts

            var content = await File.ReadAllTextAsync(file);
            var secretFindings = secretDetector.Scan(content, file);
            findings.AddRange(secretFindings);
            filesScanned++;
        }
        Console.WriteLine($" -> Scanned {filesScanned} files. Found {findings.Count(f => f.RuleId.StartsWith("SEC"))} secrets.");

        // 3. Generate Report
        Console.WriteLine("\n[3/3] Generating Report...");
        var result = new ScanResult
        {
            ScanId = Guid.NewGuid().ToString(),
            FilesScanned = filesScanned,
            Findings = findings,
            Passed = !findings.Any(),
            StartTime = start,
            EndTime = DateTime.UtcNow
        };

        var reporter = new PdfSecurityReporter();
        reporter.GenerateReport(result, reportPath);

        Console.WriteLine($"\nSUCCESS: Report generated at {reportPath}");
        Console.WriteLine($"Total Findings: {findings.Count}");
    }
}

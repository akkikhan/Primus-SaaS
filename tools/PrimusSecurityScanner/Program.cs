using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.Scanners;
using PrimusSaaS.Security.Detectors;
using PrimusSaaS.Security.Core;
using System.Diagnostics;

namespace PrimusSecurityScanner;

class Program
{
    static async Task<int> Main(string[] args)
    {
        string? scanPath = null;
        string? dbPath = null;
        string? patternsPath = null;
        string? cloudApiKey = null;
        bool updateDb = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--scan" && i + 1 < args.Length) scanPath = args[++i];
            else if (args[i] == "--db" && i + 1 < args.Length) dbPath = args[++i];
            else if (args[i] == "--patterns" && i + 1 < args.Length) patternsPath = args[++i];
            else if (args[i] == "--cloud-api-key" && i + 1 < args.Length) cloudApiKey = args[++i];
            else if (args[i] == "--update") updateDb = true;
        }

        if (updateDb)
        {
            Console.WriteLine("Updating CVE Database...");
            try
            {
                // Locate CveAggregator relative to this tool
                // Assuming we are in tools/PrimusSecurityScanner
                var aggregatorPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../tools/CveAggregator"));
                
                if (Directory.Exists(aggregatorPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"run --project \"{aggregatorPath}\" -- scrape --days 1", // Update last 1 day by default
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    
                    var process = Process.Start(psi);
                    if (process != null)
                    {
                        process.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                        process.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        await process.WaitForExitAsync();
                        return process.ExitCode;
                    }
                }
                else
                {
                    Console.WriteLine($"Error: Could not find CveAggregator at {aggregatorPath}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating database: {ex.Message}");
                return 1;
            }
            return 0;
        }

        if (string.IsNullOrEmpty(scanPath))
        {
            Console.WriteLine("Usage: PrimusSecurityScanner --scan <path> [--db <path>] [--patterns <path>] [--cloud-api-key <key>] [--update]");
            return 1;
        }

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger("Scanner");
        bool hasErrors = false;

        // 1. Secret Scanning
        if (!string.IsNullOrEmpty(patternsPath) && File.Exists(patternsPath))
        {
            logger.LogInformation("Starting Secret Scan...");
            var detector = new SecretDetector(loggerFactory.CreateLogger<SecretDetector>(), patternsPath);
            
            if (File.Exists(scanPath))
            {
                var findings = detector.Scan(File.ReadAllText(scanPath), scanPath);
                if (findings.Any()) hasErrors = true;
                foreach (var f in findings) logger.LogError("[Secret] {Title} at {Line}", f.Title, f.Line);
            }
            else if (Directory.Exists(scanPath))
            {
                foreach (var file in Directory.GetFiles(scanPath, "*.*", SearchOption.AllDirectories))
                {
                    if (file.Contains(".git") || file.Contains("bin") || file.Contains("obj")) continue;
                    
                    try 
                    {
                        var findings = detector.Scan(File.ReadAllText(file), file);
                        if (findings.Any()) hasErrors = true;
                        foreach (var f in findings) logger.LogError("[Secret] {Title} in {File}:{Line}", f.Title, file, f.Line);
                    }
                    catch {}
                }
            }
        }

        // 2. Dependency Scanning
        if (Directory.Exists(scanPath))
        {
            IVulnerabilityProvider provider;
            
            if (!string.IsNullOrEmpty(cloudApiKey))
            {
                logger.LogInformation("Using Cloud Vulnerability Provider...");
                provider = new CloudVulnerabilityProvider(new HttpClient(), cloudApiKey);
            }
            else
            {
                var db = dbPath ?? "data/cve-database/cve-database.db";
                if (File.Exists(db))
                {
                    logger.LogInformation("Using Local Vulnerability Provider ({Path})...", db);
                    provider = new LocalVulnerabilityProvider(db);
                }
                else
                {
                    logger.LogWarning("Local database not found at {Path}. Skipping dependency scan.", db);
                    provider = null;
                }
            }

            if (provider != null)
            {
                logger.LogInformation("Starting Dependency Scan...");
                var scanner = new DependencyScanner(loggerFactory.CreateLogger<DependencyScanner>(), provider);
                var findings = await scanner.ScanAsync(scanPath);
                
                if (findings.Any()) hasErrors = true;
                foreach (var f in findings)
                {
                    logger.LogError("[Dependency] [{Severity}] {Title} - {Package} {Version} ({CVE})", f.Severity, f.Title, f.Package, f.CurrentVersion, f.CVE);
                }
            }
        }

        if (hasErrors)
        {
            logger.LogError("Security issues found!");
            return 1;
        }
        else
        {
            logger.LogInformation("No security issues found.");
            return 0;
        }
    }
}

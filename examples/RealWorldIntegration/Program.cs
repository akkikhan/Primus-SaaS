using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.Scanners;
using PrimusSaaS.Security.Core;

namespace RealWorldIntegration;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Primus SaaS Security - Real World Integration Test");

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Use Local Provider
        // We need to point to the DB. In a real app, this would be distributed or downloaded.
        // Here we point to the dev DB.
        Console.WriteLine($"CWD: {Directory.GetCurrentDirectory()}");
        var dbPath = Path.GetFullPath("data/cve-database/cve-database.db");
        Console.WriteLine($"DB Path: {dbPath}");
        
        if (!File.Exists(dbPath))
        {
             Console.WriteLine($"Error: DB not found at {dbPath}");
             return;
        }

        var provider = new LocalVulnerabilityProvider(dbPath);
        
        var scanner = new DependencyScanner(loggerFactory.CreateLogger<DependencyScanner>(), provider);

        // Scan the project directory
        var sourceDir = Path.GetFullPath("examples/RealWorldIntegration");
        Console.WriteLine($"Scanning project directory: {sourceDir}");
        
        var findings = await scanner.ScanAsync(sourceDir);

        if (findings.Any())
        {
            Console.WriteLine($"\nFound {findings.Count()} vulnerabilities:");
            foreach (var f in findings)
            {
                Console.WriteLine($"[{f.Severity}] {f.Package} {f.CurrentVersion}: {f.Title}");
            }
        }
        else
        {
            Console.WriteLine("\nNo vulnerabilities found.");
        }
    }
}

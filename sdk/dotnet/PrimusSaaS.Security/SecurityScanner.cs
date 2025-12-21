using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.Core;
using PrimusSaaS.Security.Data;
using PrimusSaaS.Security.Detectors;
using PrimusSaaS.Security.Scanners;

namespace PrimusSaaS.Security;

/// <summary>
/// Main security scanner service that orchestrates all scanning components.
/// Provides unified scanning for secrets, dependencies, and code vulnerabilities.
/// </summary>
public class SecurityScanner : ISecurityScanner
{
    private readonly ILogger<SecurityScanner> _logger;
    private readonly PrimusSecurityOptions _options;
    private readonly SecretDetector? _secretDetector;
    private readonly DependencyScanner? _dependencyScanner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityScanner"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Security options.</param>
    /// <param name="loggerFactory">Logger factory for creating component loggers.</param>
    public SecurityScanner(
        ILogger<SecurityScanner> logger,
        PrimusSecurityOptions options,
        ILoggerFactory loggerFactory)
    {
        this._logger = logger;
        this._options = options;

        // Initialize secret detector if enabled
        if (options.EnableSecretDetection)
        {
            var patternsPath = Path.Combine(options.DataPath, "SecretPatterns.json");
            // If main path doesn't exist, we let FileSecretPatternProvider handle fallbacks via null
            // OR we pass the specific path if it exists.
            
            // To match previous logic: check main path, if not exists, rely on fallback inside provider or just pass null.
            string? effectivePath = File.Exists(patternsPath) ? patternsPath : null;

            var patternProvider = new FileSecretPatternProvider(
                loggerFactory.CreateLogger<FileSecretPatternProvider>(),
                effectivePath);

            this._secretDetector = new SecretDetector(
                loggerFactory.CreateLogger<SecretDetector>(),
                patternProvider);
        }

        // Initialize dependency scanner if enabled
        if (options.EnableDependencyScanning && File.Exists(options.CveDatabasePath))
        {
            IVulnerabilityProvider provider = new LocalVulnerabilityProvider(options.CveDatabasePath);
            this._dependencyScanner = new DependencyScanner(
                loggerFactory.CreateLogger<DependencyScanner>(),
                provider);
        }
    }

    /// <inheritdoc/>
    public async Task<ScanResult> ScanAsync(string path, CancellationToken cancellationToken = default)
    {
        var result = new ScanResult();
        this._logger.LogInformation("Starting security scan of: {Path}", path);

        try
        {
            // Scan for secrets in files
            if (this._secretDetector != null && this._options.EnableSecretDetection)
            {
                await this.ScanForSecretsAsync(path, result, cancellationToken);
            }

            // Scan dependencies for vulnerabilities
            if (this._dependencyScanner != null && this._options.EnableDependencyScanning)
            {
                await this.ScanDependenciesAsync(path, result, cancellationToken);
            }

            result.EndTime = DateTime.UtcNow;

            // Determine if scan passed
            result.Passed = !result.Findings.Any(f =>
                f.Severity == SecuritySeverity.Critical ||
                (this._options.FailOnCritical && f.Severity == SecuritySeverity.High));

            // Execute callbacks
            if (this._options.OnScanComplete != null)
            {
                await this._options.OnScanComplete(result);
            }

            // Handle critical vulnerabilities
            if (this._options.OnCriticalVulnerability != null)
            {
                foreach (var finding in result.Findings.Where(f => f.Severity == SecuritySeverity.Critical))
                {
                    await this._options.OnCriticalVulnerability(finding);
                }
            }

            this._logger.LogInformation(
                "Scan complete. Files: {Files}, Findings: {Findings}, Duration: {Duration:F2}s",
                result.FilesScanned,
                result.Findings.Count,
                result.Duration.TotalSeconds);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error during security scan");
            result.Errors.Add(ex.Message);
            result.Passed = false;
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<ScanResult> ScanContentAsync(string content, string fileName, CancellationToken cancellationToken = default)
    {
        var result = new ScanResult();
        result.FilesScanned = 1;

        try
        {
            // Scan content for secrets
            if (this._secretDetector != null && this._options.EnableSecretDetection)
            {
                var findings = this._secretDetector.Scan(content, fileName);
                result.Findings.AddRange(findings);
            }

            result.EndTime = DateTime.UtcNow;
            result.Passed = !result.Findings.Any(f => f.Severity == SecuritySeverity.Critical);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error scanning content: {FileName}", fileName);
            result.Errors.Add(ex.Message);
            result.Passed = false;
        }

        return result;
    }

    private async Task ScanForSecretsAsync(string path, ScanResult result, CancellationToken cancellationToken)
    {
        if (this._secretDetector == null)
        {
            return;
        }

        var filesToScan = new List<string>();

        if (File.Exists(path))
        {
            filesToScan.Add(path);
        }
        else if (Directory.Exists(path))
        {
            // Get all source files, excluding common non-source directories
            var excludedDirs = new[] { ".git", "bin", "obj", "node_modules", ".vs", ".vscode", "packages" };
            filesToScan = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(f => !excludedDirs.Any(d => f.Contains(Path.DirectorySeparatorChar + d + Path.DirectorySeparatorChar)))
                .Where(f => this.IsScannable(f))
                .ToList();
        }

        foreach (var file in filesToScan)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                var findings = this._secretDetector.Scan(content, file);
                result.Findings.AddRange(findings);
                result.FilesScanned++;
            }
            catch (Exception ex)
            {
                this._logger.LogDebug(ex, "Could not scan file: {File}", file);
            }
        }
    }

    private async Task ScanDependenciesAsync(string path, ScanResult result, CancellationToken cancellationToken)
    {
        if (this._dependencyScanner == null || !Directory.Exists(path))
        {
            return;
        }

        try
        {
            var findings = await this._dependencyScanner.ScanAsync(path);
            result.Findings.AddRange(findings);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Error scanning dependencies");
            result.Errors.Add($"Dependency scan error: {ex.Message}");
        }
    }

    private bool IsScannable(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var scannableExtensions = new[]
        {
            ".cs", ".vb", ".fs", // .NET
            ".js", ".ts", ".jsx", ".tsx", ".mjs", // JavaScript/TypeScript
            ".py", // Python
            ".java", ".kt", ".scala", // JVM
            ".go", // Go
            ".rb", // Ruby
            ".php", // PHP
            ".rs", // Rust
            ".json", ".yaml", ".yml", ".xml", ".config", // Config files
            ".env", ".properties", ".ini", // Environment files
            ".sh", ".ps1", ".bat", ".cmd", // Scripts
            ".sql", // SQL
            ".md", ".txt", // Documentation
        };

        return scannableExtensions.Contains(extension);
    }
}

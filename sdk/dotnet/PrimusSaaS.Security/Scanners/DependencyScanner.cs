using System.Xml.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.Core;
using PrimusSaaS.Security.Data;
using PrimusSaaS.Security.Models;

namespace PrimusSaaS.Security.Scanners;

/// <summary>
/// Scans project dependencies for known vulnerabilities.
/// </summary>
public class DependencyScanner
{
    private readonly ILogger<DependencyScanner> _logger;
    private readonly IVulnerabilityProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyScanner"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="provider">Vulnerability provider (Local or Cloud).</param>
    public DependencyScanner(ILogger<DependencyScanner> logger, IVulnerabilityProvider provider)
    {
        this._logger = logger;
        this._provider = provider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyScanner"/> class with Local DB.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="databasePath">Path to the CVE database.</param>
    public DependencyScanner(ILogger<DependencyScanner> logger, string databasePath)
        : this(logger, new LocalVulnerabilityProvider(databasePath))
    {
    }

    /// <summary>
    /// Scans the project for vulnerable dependencies.
    /// </summary>
    /// <param name="projectPath">Root path of the project to scan.</param>
    /// <returns>List of security findings.</returns>
    public async Task<IEnumerable<SecurityFinding>> ScanAsync(string projectPath)
    {
        var findings = new List<SecurityFinding>();
        var dependencies = new List<Dependency>();

        // Find .csproj files (NuGet)
        try
        {
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
            foreach (var file in csprojFiles)
            {
                dependencies.AddRange(this.ParseCsproj(file));
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Error scanning for .csproj files in {Path}", projectPath);
        }

        // Find package.json files (NPM)
        try
        {
            var packageJsonFiles = Directory.GetFiles(projectPath, "package.json", SearchOption.AllDirectories);
            foreach (var file in packageJsonFiles)
            {
                if (!file.Contains("node_modules")) // Skip node_modules
                {
                    dependencies.AddRange(this.ParsePackageJson(file));
                }
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Error scanning for package.json files in {Path}", projectPath);
        }

        // Find requirements.txt files (Python)
        try
        {
            var reqFiles = Directory.GetFiles(projectPath, "requirements.txt", SearchOption.AllDirectories);
            foreach (var file in reqFiles)
            {
                if (!file.Contains("venv") && !file.Contains(".env"))
                {
                    dependencies.AddRange(this.ParseRequirementsTxt(file));
                }
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Error scanning for requirements.txt files in {Path}", projectPath);
        }

        // Find pom.xml files (Java)
        try
        {
            var pomFiles = Directory.GetFiles(projectPath, "pom.xml", SearchOption.AllDirectories);
            foreach (var file in pomFiles)
            {
                dependencies.AddRange(this.ParsePomXml(file));
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Error scanning for pom.xml files in {Path}", projectPath);
        }

        foreach (var dep in dependencies)
        {
            try
            {
                var vulns = await this._provider.CheckPackageAsync(dep);
                foreach (var vuln in vulns)
                {
                    vuln.FilePath = dep.FilePath;
                    findings.Add(vuln);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Error checking package {Package}", dep.Name);
            }
        }

        return findings;
    }

    private IEnumerable<Dependency> ParseCsproj(string filePath)
    {
        var deps = new List<Dependency>();
        try
        {
            var doc = XDocument.Load(filePath);
            var packageRefs = doc.Descendants("PackageReference");
            foreach (var elem in packageRefs)
            {
                var name = elem.Attribute("Include")?.Value;
                var version = elem.Attribute("Version")?.Value;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(version))
                {
                    this._logger.LogInformation("Found dependency: {Name} {Version}", name, version);
                    deps.Add(new Dependency
                    {
                        Name = name,
                        Version = version,
                        Ecosystem = "nuget",
                        FilePath = filePath,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to parse .csproj file: {Path}", filePath);
        }

        return deps;
    }

    private IEnumerable<Dependency> ParsePackageJson(string filePath)
    {
        var deps = new List<Dependency>();
        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("dependencies", out var dependencies))
            {
                foreach (var prop in dependencies.EnumerateObject())
                {
                    deps.Add(new Dependency
                    {
                        Name = prop.Name,
                        Version = prop.Value.GetString() ?? "",
                        Ecosystem = "npm",
                        FilePath = filePath,
                    });
                }
            }

            if (root.TryGetProperty("devDependencies", out var devDependencies))
            {
                foreach (var prop in devDependencies.EnumerateObject())
                {
                    deps.Add(new Dependency
                    {
                        Name = prop.Name,
                        Version = prop.Value.GetString() ?? "",
                        Ecosystem = "npm",
                        FilePath = filePath,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to parse package.json file: {Path}", filePath);
        }

        return deps;
    }

    private IEnumerable<Dependency> ParseRequirementsTxt(string filePath)
    {
        var deps = new List<Dependency>();
        try
        {
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                // Simple parser for "package==version" or "package>=version"
                // We split by common operators
                var parts = trimmed.Split(new[] { "==", ">=", "<=", ">", "<", "~=" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var name = parts[0].Trim();
                    var version = parts[1].Trim(); // This might be "1.0.0" or "1.0.0, <2.0"
                    // Take first part of version if comma exists
                    version = version.Split(',')[0].Trim();

                    deps.Add(new Dependency
                    {
                        Name = name,
                        Version = version,
                        Ecosystem = "pypi",
                        FilePath = filePath,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to parse requirements.txt file: {Path}", filePath);
        }
        return deps;
    }

    private IEnumerable<Dependency> ParsePomXml(string filePath)
    {
        var deps = new List<Dependency>();
        try
        {
            var doc = XDocument.Load(filePath);
            // Maven namespace is tricky, usually xmlns="http://maven.apache.org/POM/4.0.0"
            // We'll use local name to be safe
            var dependencies = doc.Descendants().Where(e => e.Name.LocalName == "dependency");
            
            foreach (var dep in dependencies)
            {
                var groupId = dep.Elements().FirstOrDefault(e => e.Name.LocalName == "groupId")?.Value;
                var artifactId = dep.Elements().FirstOrDefault(e => e.Name.LocalName == "artifactId")?.Value;
                var version = dep.Elements().FirstOrDefault(e => e.Name.LocalName == "version")?.Value;

                if (!string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(artifactId) && !string.IsNullOrEmpty(version))
                {
                    // Java packages are usually GroupId:ArtifactId
                    deps.Add(new Dependency
                    {
                        Name = $"{groupId}:{artifactId}",
                        Version = version,
                        Ecosystem = "maven",
                        FilePath = filePath,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to parse pom.xml file: {Path}", filePath);
        }
        return deps;
    }
}

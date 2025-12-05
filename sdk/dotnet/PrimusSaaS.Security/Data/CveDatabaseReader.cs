using Microsoft.Data.Sqlite;
using NuGet.Versioning;
using PrimusSaaS.Security.Core;

namespace PrimusSaaS.Security.Data;

/// <summary>
/// Reads vulnerability data from the local CVE database.
/// </summary>
public class CveDatabaseReader
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="CveDatabaseReader"/> class.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database.</param>
    public CveDatabaseReader(string databasePath)
    {
        this._connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly }.ToString();
    }

    /// <summary>
    /// Checks a package for known vulnerabilities.
    /// </summary>
    /// <param name="packageName">Name of the package.</param>
    /// <param name="version">Version of the package.</param>
    /// <param name="ecosystem">Ecosystem (nuget, npm).</param>
    /// <returns>List of security findings.</returns>
    public async Task<List<SecurityFinding>> CheckPackageAsync(string packageName, string version, string ecosystem)
    {
        var findings = new List<SecurityFinding>();
        
        if (!File.Exists(new SqliteConnectionStringBuilder(this._connectionString).DataSource))
        {
            return findings;
        }

        using var connection = new SqliteConnection(this._connectionString);
        await connection.OpenAsync();

        // Heuristic:
        // 1. Exact match (case insensitive)
        // 2. "vendor:package" match
        // 3. Product name match (last part of package name)
        
        var searchName = packageName.ToLowerInvariant();
        var parts = searchName.Split(new[] { '.', '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
        var product = parts.Last();
        
        var query = @"
            SELECT v.cve_id, v.description, v.severity, v.cvss_v3_score, p.version_range, p.package_name, v.reference_urls
            FROM affected_packages p
            JOIN vulnerabilities v ON p.vulnerability_id = v.id
            WHERE p.package_name LIKE @namePattern 
               OR p.package_name LIKE @namePattern2
               OR p.package_name LIKE @productPattern
               OR p.package_name LIKE @productPattern2
        ";

        using var cmd = connection.CreateCommand();
        cmd.CommandText = query;
        cmd.Parameters.AddWithValue("@namePattern", $"%:{searchName}"); 
        cmd.Parameters.AddWithValue("@namePattern2", $"%:{searchName}:%");
        cmd.Parameters.AddWithValue("@productPattern", $"%:{product}");
        cmd.Parameters.AddWithValue("@productPattern2", $"%:{product}:%");

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var range = reader.GetString(4);
            if (this.IsVersionAffected(version, range))
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = $"Vulnerable Dependency: {packageName} ({reader.GetString(0)})",
                    Description = reader.GetString(1),
                    Severity = this.ParseSeverity(reader.GetString(2)),
                    CVE = reader.GetString(0),
                    CVSSScore = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                    Package = packageName,
                    CurrentVersion = version,
                    Remediation = $"Update {packageName} to a fixed version. Affected range: {range}",
                    RuleId = "DEP-001",
                });
            }
        }

        return findings;
    }

    private bool IsVersionAffected(string versionStr, string rangeStr)
    {
        if (string.IsNullOrEmpty(rangeStr) || rangeStr == "*") return true;

        try
        {
            if (!NuGetVersion.TryParse(versionStr, out var version))
            {
                if (Version.TryParse(versionStr, out var v))
                {
                    version = new NuGetVersion(v);
                }
                else
                {
                    return true; // Fail safe
                }
            }

            var conditions = rangeStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var condition in conditions)
            {
                if (condition.StartsWith(">="))
                {
                    if (!NuGetVersion.TryParse(condition.Substring(2), out var v) || version < v) return false;
                }
                else if (condition.StartsWith(">"))
                {
                    if (!NuGetVersion.TryParse(condition.Substring(1), out var v) || version <= v) return false;
                }
                else if (condition.StartsWith("<="))
                {
                    if (!NuGetVersion.TryParse(condition.Substring(2), out var v) || version > v) return false;
                }
                else if (condition.StartsWith("<"))
                {
                    if (!NuGetVersion.TryParse(condition.Substring(1), out var v) || version >= v) return false;
                }
                else if (condition.StartsWith("="))
                {
                    if (!NuGetVersion.TryParse(condition.Substring(1), out var v) || version != v) return false;
                }
            }
            
            return true;
        }
        catch
        {
            return true;
        }
    }

    private SecuritySeverity ParseSeverity(string severity)
    {
        return Enum.TryParse<SecuritySeverity>(severity, true, out var s) ? s : SecuritySeverity.Medium;
    }
}

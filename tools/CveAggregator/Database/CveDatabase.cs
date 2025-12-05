using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.CveAggregator.Models;

namespace PrimusSaaS.Security.CveAggregator.Database;

public interface ICveDatabase : IDisposable
{
    Task InitializeAsync(string databasePath);
    Task InsertVulnerabilitiesAsync(List<Vulnerability> vulnerabilities);
    Task<int> GetVulnerabilityCountAsync();
    Task<Dictionary<string, int>> GetStatisticsAsync();
}

public class CveDatabase : ICveDatabase
{
    private readonly ILogger<CveDatabase> _logger;
    private SqliteConnection? _connection;

    public CveDatabase(ILogger<CveDatabase> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(string databasePath)
    {
        _logger.LogInformation("🔨 Initializing CVE database: {Path}", databasePath);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("   Created directory: {Directory}", directory);
        }

        // Create connection
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        };
        _connection = new SqliteConnection(builder.ToString());
        await _connection.OpenAsync();

        _logger.LogInformation("   Database connection opened");

        // Create basic schema
        await CreateBasicSchemaAsync();

        _logger.LogInformation("✅ Database initialized successfully");
    }

    private async Task CreateBasicSchemaAsync()
    {
        _logger.LogInformation("   Creating database schema...");

        // Create vulnerabilities table
        var tableSql = "CREATE TABLE IF NOT EXISTS vulnerabilities (id INTEGER PRIMARY KEY, cve_id TEXT NOT NULL UNIQUE, description TEXT, cvss_v3_score REAL, cvss_v3_vector TEXT, severity TEXT, published_date TEXT, last_modified TEXT, cwe_id TEXT, source TEXT, reference_urls TEXT)";
        
        try
        {
            using (var cmd = _connection!.CreateCommand())
            {
                _logger.LogInformation("   Executing SQL: {Sql}", tableSql);
                cmd.CommandText = tableSql;
                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("   ✅ Created vulnerabilities table");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "   ❌ Failed to create table. SQL: {Sql}", tableSql);
            throw;
        }

        // Create affected_packages table
        var packagesTableSql = "CREATE TABLE IF NOT EXISTS affected_packages (id INTEGER PRIMARY KEY, vulnerability_id INTEGER NOT NULL, ecosystem TEXT, package_name TEXT NOT NULL, version_range TEXT, FOREIGN KEY(vulnerability_id) REFERENCES vulnerabilities(id))";
        
        try
        {
            using (var cmd = _connection!.CreateCommand())
            {
                cmd.CommandText = packagesTableSql;
                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("   ✅ Created affected_packages table");
            }
            
            using (var cmd = _connection!.CreateCommand())
            {
                cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_packages_name ON affected_packages(package_name)";
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "   ❌ Failed to create affected_packages table");
            throw;
        }

        // Create indexes
        try 
        {
            using (var cmd = _connection!.CreateCommand())
            {
                var indexSql = "CREATE INDEX IF NOT EXISTS idx_cve_id ON vulnerabilities(cve_id)";
                _logger.LogInformation("   Executing SQL: {Sql}", indexSql);
                cmd.CommandText = indexSql;
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = _connection!.CreateCommand())
            {
                var indexSql = "CREATE INDEX IF NOT EXISTS idx_severity ON vulnerabilities(severity)";
                _logger.LogInformation("   Executing SQL: {Sql}", indexSql);
                cmd.CommandText = indexSql;
                await cmd.ExecuteNonQueryAsync();
            }
            _logger.LogInformation("   ✅ Created indexes");
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "   ❌ Failed to create indexes");
        }
    }

    private async Task ExecuteSchemaAsync(string schema)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var statements = schema.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        var executed = 0;

        foreach (var statement in statements)
        {
            var trimmed = statement.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("--"))
                continue;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = trimmed;
            await cmd.ExecuteNonQueryAsync();
            executed++;
        }

        _logger.LogInformation("   Executed {Count} SQL statements", executed);
    }

    public async Task InsertVulnerabilitiesAsync(List<Vulnerability> vulnerabilities)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        _logger.LogInformation("💾 Inserting {Count} vulnerabilities...", vulnerabilities.Count);

        var inserted = 0;
        var updated = 0;
        var skipped = 0;

        using var transaction = _connection.BeginTransaction();

        try
        {
            foreach (var vuln in vulnerabilities)
            {
                try
                {
                    var existingId = await GetVulnerabilityIdAsync(vuln.CveId);

                    if (existingId.HasValue)
                    {
                        await UpdateVulnerabilityAsync(existingId.Value, vuln);
                        updated++;
                    }
                    else
                    {
                        await InsertVulnerabilityAsync(vuln);
                        inserted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("   ⚠️  Failed to insert/update {CveId}: {Error}", 
                        vuln.CveId, ex.Message);
                    skipped++;
                }
            }

            await transaction.CommitAsync();

            _logger.LogInformation("✅ Database updated:");
            _logger.LogInformation("   Inserted: {Inserted}", inserted);
            _logger.LogInformation("   Updated: {Updated}", updated);
            if (skipped > 0)
                _logger.LogInformation("   Skipped: {Skipped}", skipped);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "❌ Failed to insert vulnerabilities");
            throw;
        }
    }

    private async Task<long?> GetVulnerabilityIdAsync(string cveId)
    {
        if (_connection == null) return null;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id FROM vulnerabilities WHERE cve_id = @cveId";
        cmd.Parameters.AddWithValue("@cveId", cveId);

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt64(result) : null;
    }

    private async Task InsertVulnerabilityAsync(Vulnerability vuln)
    {
        if (_connection == null) return;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
INSERT INTO vulnerabilities (
    cve_id, description, cvss_v3_score, cvss_v3_vector,
    severity, published_date, last_modified, cwe_id, source, reference_urls
) VALUES (
    @cveId, @description, @cvssScore, @cvssVector,
    @severity, @published, @modified, @cwe, @source, @references
)";

        cmd.Parameters.AddWithValue("@cveId", vuln.CveId);
        cmd.Parameters.AddWithValue("@description", vuln.Description ?? "");
        cmd.Parameters.AddWithValue("@cvssScore", vuln.CVSSv3Score.HasValue ? (object)vuln.CVSSv3Score.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@cvssVector", vuln.CVSSv3Vector ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@severity", vuln.Severity);
        cmd.Parameters.AddWithValue("@published", vuln.PublishedDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@modified", vuln.LastModified?.ToString("yyyy-MM-ddTHH:mm:ss") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@cwe", vuln.CWE ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@source", vuln.Source);
        cmd.Parameters.AddWithValue("@references", string.Join(",", vuln.References));



        cmd.CommandText += "; SELECT last_insert_rowid();";
        var result = await cmd.ExecuteScalarAsync();
        var vulnId = result != null ? Convert.ToInt64(result) : 0;

        if (vulnId > 0 && vuln.AffectedPackages != null && vuln.AffectedPackages.Any())
        {
            using var pkgCmd = _connection.CreateCommand();
            pkgCmd.CommandText = "INSERT INTO affected_packages (vulnerability_id, ecosystem, package_name, version_range) VALUES (@vulnId, @ecosystem, @package, @version)";
            
            var pVulnId = pkgCmd.CreateParameter(); pVulnId.ParameterName = "@vulnId"; pkgCmd.Parameters.Add(pVulnId);
            var pEco = pkgCmd.CreateParameter(); pEco.ParameterName = "@ecosystem"; pkgCmd.Parameters.Add(pEco);
            var pPkg = pkgCmd.CreateParameter(); pPkg.ParameterName = "@package"; pkgCmd.Parameters.Add(pPkg);
            var pVer = pkgCmd.CreateParameter(); pVer.ParameterName = "@version"; pkgCmd.Parameters.Add(pVer);

            foreach (var pkg in vuln.AffectedPackages)
            {
                pVulnId.Value = vulnId;
                pEco.Value = pkg.Ecosystem ?? "";
                pPkg.Value = pkg.PackageName ?? "";
                pVer.Value = pkg.AffectedVersionRange ?? "";
                await pkgCmd.ExecuteNonQueryAsync();
            }
        }
    }

    private async Task UpdateVulnerabilityAsync(long id, Vulnerability vuln)
    {
        if (_connection == null) return;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
UPDATE vulnerabilities SET
    description = @description,
    cvss_v3_score = @cvssScore,
    cvss_v3_vector = @cvssVector,
    severity = @severity,
    last_modified = @modified,
    cwe_id = @cwe,
    reference_urls = @references
WHERE id = @id";

        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@description", vuln.Description ?? "");
        cmd.Parameters.AddWithValue("@cvssScore", vuln.CVSSv3Score.HasValue ? (object)vuln.CVSSv3Score.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@cvssVector", vuln.CVSSv3Vector ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@severity", vuln.Severity);
        cmd.Parameters.AddWithValue("@modified", vuln.LastModified?.ToString("yyyy-MM-ddTHH:mm:ss") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@cwe", vuln.CWE ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@references", string.Join(",", vuln.References));

        await cmd.ExecuteNonQueryAsync();

        // Update packages: Delete old, insert new
        using var delCmd = _connection.CreateCommand();
        delCmd.CommandText = "DELETE FROM affected_packages WHERE vulnerability_id = @id";
        delCmd.Parameters.AddWithValue("@id", id);
        await delCmd.ExecuteNonQueryAsync();

        if (vuln.AffectedPackages != null && vuln.AffectedPackages.Any())
        {
            using var pkgCmd = _connection.CreateCommand();
            pkgCmd.CommandText = "INSERT INTO affected_packages (vulnerability_id, ecosystem, package_name, version_range) VALUES (@vulnId, @ecosystem, @package, @version)";
            
            var pVulnId = pkgCmd.CreateParameter(); pVulnId.ParameterName = "@vulnId"; pkgCmd.Parameters.Add(pVulnId);
            var pEco = pkgCmd.CreateParameter(); pEco.ParameterName = "@ecosystem"; pkgCmd.Parameters.Add(pEco);
            var pPkg = pkgCmd.CreateParameter(); pPkg.ParameterName = "@package"; pkgCmd.Parameters.Add(pPkg);
            var pVer = pkgCmd.CreateParameter(); pVer.ParameterName = "@version"; pkgCmd.Parameters.Add(pVer);

            foreach (var pkg in vuln.AffectedPackages)
            {
                pVulnId.Value = id;
                pEco.Value = pkg.Ecosystem ?? "";
                pPkg.Value = pkg.PackageName ?? "";
                pVer.Value = pkg.AffectedVersionRange ?? "";
                await pkgCmd.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<int> GetVulnerabilityCountAsync()
    {
        if (_connection == null) return 0;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM vulnerabilities";

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public async Task<Dictionary<string, int>> GetStatisticsAsync()
    {
        if (_connection == null) return new Dictionary<string, int>();

        var stats = new Dictionary<string, int>();

        // Count by source
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT source, COUNT(*) as count FROM vulnerabilities GROUP BY source";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var source = reader.GetString(0);
                var count = reader.GetInt32(1);
                stats[source] = count;
            }
        }

        // Count by severity
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT severity, COUNT(*) as count FROM vulnerabilities GROUP BY severity";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var severity = reader.GetString(0);
                var count = reader.GetInt32(1);
                stats[$"Severity_{severity}"] = count;
            }
        }

        // Count affected packages
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM affected_packages";
            var count = await cmd.ExecuteScalarAsync();
            stats["AffectedPackages"] = count != null ? Convert.ToInt32(count) : 0;
        }

        return stats;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}


using System.Text.Json;
using Microsoft.Data.Sqlite;
using PrimusSaaS.Security.DataAggregator.Models;

namespace PrimusSaaS.Security.DataAggregator;

public class DatabaseBuilder
{
    private readonly string _schemaPath;
    private readonly string _outputPath;

    public DatabaseBuilder(string schemaPath, string outputPath)
    {
        _schemaPath = schemaPath;
        _outputPath = outputPath;
    }

    public void Initialize()
    {
        if (File.Exists(_outputPath))
        {
            File.Delete(_outputPath);
        }

        var schema = File.ReadAllText(_schemaPath);
        using var connection = new SqliteConnection($"Data Source={_outputPath}");
        connection.Open();
        
        // Execute schema script
        // Attempt to execute the entire script at once. 
        // Microsoft.Data.Sqlite supports multiple statements if they are just text.
        using var command = connection.CreateCommand();
        command.CommandText = schema;
        command.ExecuteNonQuery();
    }

    public async Task ProcessAdvisoriesAsync(string rootDirectory)
    {
        Console.WriteLine($"Scanning for JSON files in {rootDirectory}...");
        var files = Directory.GetFiles(rootDirectory, "*.json", SearchOption.AllDirectories);
        Console.WriteLine($"Found {files.Length} files.");

        using var connection = new SqliteConnection($"Data Source={_outputPath}");
        connection.Open();

        using var transaction = connection.BeginTransaction();

        int count = 0;
        foreach (var file in files)
        {
            try 
            {
                var json = await File.ReadAllTextAsync(file);
                var advisory = JsonSerializer.Deserialize<SecurityAdvisory>(json);

                if (advisory != null && !advisory.Id.StartsWith("GHSA")) 
                {
                   // Some files might be metadata, skip
                   continue;
                }
                
                if (advisory != null)
                {
                    InsertAdvisory(connection, transaction, advisory);
                    count++;
                }

                if (count % 1000 == 0) Console.Write(".");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {file}: {ex.Message}");
            }
        }
        
        transaction.Commit();
        Console.WriteLine($"\nImported {count} advisories.");
    }

    private void InsertAdvisory(SqliteConnection conn, SqliteTransaction trans, SecurityAdvisory ad)
    {
        // Insert Vulnerability
        var insertVuln = @"
            INSERT INTO vulnerabilities (
                cve_id, description, severity, published_date, last_modified, source, reference_urls, cvss_v3_score
            ) VALUES (
                @cveId, @desc, @severity, @pub, @mod, 'GitHub', @refs, 0.0
            );
            SELECT last_insert_rowid();";

        // Prefer CVE alias if available, else GHSA ID
        var cveId = ad.Aliases.FirstOrDefault(a => a.StartsWith("CVE")) ?? ad.Id;
        
        using var cmd = conn.CreateCommand();
        cmd.Transaction = trans;
        cmd.CommandText = insertVuln;
        cmd.Parameters.AddWithValue("@cveId", cveId);
        cmd.Parameters.AddWithValue("@desc", ad.Summary); // Using Summary as checks use Description field
        cmd.Parameters.AddWithValue("@severity", ad.Severity.ToUpper());
        cmd.Parameters.AddWithValue("@pub", ad.Published?.ToString("O") ?? DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@mod", ad.Modified?.ToString("O") ?? DateTime.UtcNow.ToString("O"));
        
        var refs = ad.References.Select(r => r.Url).ToList();
        cmd.Parameters.AddWithValue("@refs", JsonSerializer.Serialize(refs));

        long vulnId = (long)cmd.ExecuteScalar()!;

        // Insert Affected Packages
        foreach (var affected in ad.Affected)
        {
            foreach (var range in affected.Ranges)
            {
                // Convert GHSA range events to Semantic Version Range string
                var rangeStr = ConvertEventsToRange(range.Events);
                var fixedVersion = range.Events.FirstOrDefault(e => !string.IsNullOrEmpty(e.Fixed))?.Fixed;

                var insertPkg = @"
                    INSERT INTO affected_packages (
                        vulnerability_id, ecosystem, package_name, affected_version_range, patched_version
                    ) VALUES (
                        @vulnId, @eco, @pkg, @range, @patched
                    )";

                using var cmdPkg = conn.CreateCommand();
                cmdPkg.Transaction = trans;
                cmdPkg.CommandText = insertPkg;
                cmdPkg.Parameters.AddWithValue("@vulnId", vulnId);
                cmdPkg.Parameters.AddWithValue("@eco", affected.Package.Ecosystem.ToLowerInvariant());
                cmdPkg.Parameters.AddWithValue("@pkg", affected.Package.Name);
                cmdPkg.Parameters.AddWithValue("@range", rangeStr);
                cmdPkg.Parameters.AddWithValue("@patched", (object?)fixedVersion ?? DBNull.Value);
                cmdPkg.ExecuteNonQuery();
            }
        }
    }

    private string ConvertEventsToRange(List<RangeEvent> events)
    {
        // Simple converter for Introduced/Fixed events to NuGet/SemVer range
        // Introduced: 0, Fixed: 1.2.3 -> (, 1.2.3)
        // Introduced: 1.0.0, Fixed: 1.2.3 -> [1.0.0, 1.2.3)
        
        var introduced = events.FirstOrDefault(e => !string.IsNullOrEmpty(e.Introduced))?.Introduced;
        var fixedVer = events.FirstOrDefault(e => !string.IsNullOrEmpty(e.Fixed))?.Fixed;

        if (string.IsNullOrEmpty(introduced) || introduced == "0")
        {
            if (!string.IsNullOrEmpty(fixedVer))
            {
                return $"(, {fixedVer})"; // Up to fixed (exclusive)
            }
            return "*"; // All versions
        }

        if (!string.IsNullOrEmpty(fixedVer))
        {
            return $"[{introduced}, {fixedVer})"; // From introduced (incl) to fixed (excl)
        }

        return $"[{introduced}, )"; // Introduced onwards
    }
    
    // Helper to split SQL by ; safely-ish (ignores quoted semicolons logic for now, assuming clean schema)
    private IEnumerable<string> SplitSqlStatements(string sqlScript)
    {
        return sqlScript.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
             .Select(s => s.Trim())
             .Where(s => s.Length > 0);
    }
}

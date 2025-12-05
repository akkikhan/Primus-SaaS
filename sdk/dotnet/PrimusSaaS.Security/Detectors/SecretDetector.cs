using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.Core;

namespace PrimusSaaS.Security.Detectors;

/// <summary>
/// Detects hardcoded secrets using regex patterns and entropy analysis.
/// </summary>
public class SecretDetector
{
    private readonly ILogger<SecretDetector> _logger;
    private readonly List<SecretPattern> _patterns;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretDetector"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="patternsJsonPath">Optional path to patterns file.</param>
    public SecretDetector(ILogger<SecretDetector> logger, string? patternsJsonPath = null)
    {
        this._logger = logger;
        this._patterns = this.LoadPatterns(patternsJsonPath);
    }

    /// <summary>
    /// Scans the provided content for secrets.
    /// </summary>
    /// <param name="content">Content to scan.</param>
    /// <param name="filePath">Path of the file being scanned.</param>
    /// <returns>List of security findings.</returns>
    public IEnumerable<SecurityFinding> Scan(string content, string filePath)
    {
        var findings = new List<SecurityFinding>();

        foreach (var pattern in this._patterns)
        {
            try
            {
                var regex = new Regex(pattern.Pattern, RegexOptions.Compiled | RegexOptions.Multiline);
                var matches = regex.Matches(content);

                foreach (Match match in matches)
                {
                    if (pattern.EntropyThreshold.HasValue)
                    {
                        var entropy = this.CalculateEntropy(match.Value);
                        if (entropy < pattern.EntropyThreshold.Value)
                        {
                            continue; // Skip low entropy matches
                        }
                    }

                    findings.Add(new SecurityFinding
                    {
                        Id = Guid.NewGuid().ToString(),
                        RuleId = pattern.Id,
                        Title = $"Hardcoded Secret: {pattern.Type}",
                        Description = pattern.Description,
                        Severity = this.ParseSeverity(pattern.Severity),
                        FilePath = filePath,
                        Line = this.GetLineNumber(content, match.Index),
                        Code = this.GetContextSnippet(content, match),
                        Remediation = pattern.Remediation,
                        CWE = pattern.Cwe,
                        OWASP = pattern.Owasp,
                    });
                }
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Error scanning pattern {Id}", pattern.Id);
            }
        }

        return findings;
    }

    private List<SecretPattern> LoadPatterns(string? path)
    {
        try 
        {
            string json;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                json = File.ReadAllText(path);
            }
            else
            {
                // Fallback: Try to load from default location relative to execution
                var defaultPath = Path.Combine(AppContext.BaseDirectory, "Data", "SecretPatterns.json");
                if (File.Exists(defaultPath))
                {
                    json = File.ReadAllText(defaultPath);
                }
                else
                {
                    // Try one more level up (for test projects)
                    defaultPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "SecretPatterns.json");
                    if (File.Exists(defaultPath))
                    {
                        json = File.ReadAllText(defaultPath);
                    }
                    else
                    {
                        this._logger.LogWarning("Secret patterns file not found at {Path} or default locations.", path);
                        return new List<SecretPattern>();
                    }
                }
            }
            
            var config = JsonSerializer.Deserialize<SecretPatternsConfig>(json);
            return config?.Patterns ?? new List<SecretPattern>();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to load secret patterns");
            return new List<SecretPattern>();
        }
    }

    private SecuritySeverity ParseSeverity(string severity)
    {
        if (Enum.TryParse<SecuritySeverity>(severity, true, out var result))
        {
            return result;
        }

        return SecuritySeverity.Medium;
    }

    private int GetLineNumber(string content, int index)
    {
        int lines = 1;
        for (int i = 0; i < index && i < content.Length; i++)
        {
            if (content[i] == '\n') 
            {
                lines++;
            }
        }

        return lines;
    }

    private double CalculateEntropy(string s)
    {
        if (string.IsNullOrEmpty(s)) 
        {
            return 0;
        }
        
        var map = new Dictionary<char, int>();
        foreach (char c in s)
        {
            if (!map.ContainsKey(c)) 
            {
                map[c] = 0;
            }

            map[c]++;
        }

        double entropy = 0;
        foreach (var item in map)
        {
            double frequency = (double)item.Value / s.Length;
            entropy -= frequency * Math.Log2(frequency);
        }

        return entropy;
    }

    private string GetContextSnippet(string content, Match match)
    {
        try
        {
            int lineStart = content.LastIndexOf('\n', match.Index);
            if (lineStart == -1) 
            {
                lineStart = 0;
            }
            else 
            {
                lineStart++; // Skip \n
            }

            int lineEnd = content.IndexOf('\n', match.Index + match.Length);
            if (lineEnd == -1) 
            {
                lineEnd = content.Length;
            }

            // Extract the full line
            string line = content.Substring(lineStart, lineEnd - lineStart).Trim();
            
            // For simplicity in snippet, we'll just return the line with redacted secret
            return line.Replace(match.Value, "***REDACTED***");
        }
        catch
        {
            return "***REDACTED***";
        }
    }
}

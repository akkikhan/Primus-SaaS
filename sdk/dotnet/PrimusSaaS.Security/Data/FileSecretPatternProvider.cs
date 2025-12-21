using System.Text.Json;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.Core;
using PrimusSaaS.Security.Detectors;

namespace PrimusSaaS.Security.Data;

/// <summary>
/// Loads secret patterns from a JSON file.
/// </summary>
public class FileSecretPatternProvider : ISecretPatternProvider
{
    private readonly ILogger<FileSecretPatternProvider> _logger;
    private readonly string? _customPath;

    public FileSecretPatternProvider(ILogger<FileSecretPatternProvider> logger, string? customPath = null)
    {
        _logger = logger;
        _customPath = customPath;
    }

    public IEnumerable<SecretPattern> GetPatterns()
    {
        try 
        {
            string? json = null;

            // 1. Try custom path if provided
            if (!string.IsNullOrEmpty(_customPath) && File.Exists(_customPath))
            {
                _logger.LogDebug("Loading secret patterns from custom path: {Path}", _customPath);
                json = File.ReadAllText(_customPath);
            }

            // 2. Fallback to Embedded Resource
            if (string.IsNullOrEmpty(json))
            {
                var assembly = typeof(FileSecretPatternProvider).Assembly;
                
                // Find resource ending with SecretPatterns.json (handles subtle namespace/folder changes)
                var resourceNames = assembly.GetManifestResourceNames();
                var resourceName = resourceNames
                    .FirstOrDefault(n => n.EndsWith("SecretPatterns.json", StringComparison.OrdinalIgnoreCase));
                
                if (resourceName != null)
                {
                    _logger.LogDebug("Loading secret patterns from embedded resource: {Name}", resourceName);
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    using var reader = new StreamReader(stream!);
                    json = reader.ReadToEnd();
                }
                else
                {
                    // List available resources to help debugging
                    var resources = string.Join(", ", resourceNames);
                    _logger.LogWarning("Embedded secret patterns not found. Available resources: {Resources}", resources);
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                return new List<SecretPattern>();
            }
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var config = JsonSerializer.Deserialize<SecretPatternsConfig>(json, options);
            return config?.Patterns ?? new List<SecretPattern>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load secret patterns");
            return new List<SecretPattern>();
        }
    }
}

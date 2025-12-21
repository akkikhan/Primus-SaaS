using PrimusSaaS.Security.Detectors;

namespace PrimusSaaS.Security.Core;

/// <summary>
/// Provider for secret patterns.
/// </summary>
public interface ISecretPatternProvider
{
    /// <summary>
    /// Gets the list of secret patterns.
    /// </summary>
    /// <returns>List of patterns.</returns>
    IEnumerable<SecretPattern> GetPatterns();
}

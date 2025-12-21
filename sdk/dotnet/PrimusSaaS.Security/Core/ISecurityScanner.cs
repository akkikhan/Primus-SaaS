using PrimusSaaS.Security.Core;

namespace PrimusSaaS.Security;

/// <summary>
/// Defines the contract for the main security scanning service.
/// </summary>
public interface ISecurityScanner
{
    /// <summary>
    /// Scans a directory or file for security vulnerabilities.
    /// </summary>
    /// <param name="path">Path to scan (file or directory).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Scan result containing all findings.</returns>
    Task<ScanResult> ScanAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans the provided source code content for security issues.
    /// </summary>
    /// <param name="content">Source code content to scan.</param>
    /// <param name="fileName">Name of the file (for reporting).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Scan result containing all findings.</returns>
    Task<ScanResult> ScanContentAsync(string content, string fileName, CancellationToken cancellationToken = default);
}

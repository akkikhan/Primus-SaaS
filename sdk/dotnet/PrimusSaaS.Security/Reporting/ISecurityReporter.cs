
using PrimusSaaS.Security.Core;

namespace PrimusSaaS.Security.Reporting;

/// <summary>
/// Interface for generating security scan reports.
/// </summary>
public interface ISecurityReporter
{
    /// <summary>
    /// Generates a report from the scan result.
    /// </summary>
    /// <param name="result">The scan result to report.</param>
    /// <param name="outputPath">The file path where the report should be saved.</param>
    void GenerateReport(ScanResult result, string outputPath);
}

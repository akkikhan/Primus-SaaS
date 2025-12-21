using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using PrimusSaaS.Security.Core;
using PrimusSaaS.Security.Reporting;
using Xunit;

namespace PrimusSaaS.Security.Tests;

public class ReportingTests
{
    [Fact]
    public void GenerateReport_CreatesValidPdfFile()
    {
        // Arrange
        var findings = new List<SecurityFinding>
        {
            new SecurityFinding
            {
                Id = "TEST-001",
                Title = "Hardcoded Secret",
                Description = "A specific API key was found.",
                Severity = SecuritySeverity.High,
                FilePath = "Program.cs",
                Line = 42,
                RuleId = "SEC-001"
            },
            new SecurityFinding
            {
                Id = "TEST-002",
                Title = "Weak Algorithm",
                Description = "MD5 usage detected.",
                Severity = SecuritySeverity.Medium,
                FilePath = "Utils.cs",
                Line = 15,
                RuleId = "CRYPTO-005"
            }
        };

        var result = new ScanResult
        {
            ScanId = "test-scan-123",
            FilesScanned = 10,
            Findings = findings,
            Passed = false
        };

        var outputPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid()}.pdf");
        var reporter = new PdfSecurityReporter();

        try
        {
            // Act
            reporter.GenerateReport(result, outputPath);

            // Assert
            File.Exists(outputPath).Should().BeTrue();
            var fileInfo = new FileInfo(outputPath);
            fileInfo.Length.Should().BeGreaterThan(1024); // PDF header + content should be > 1KB
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }
}

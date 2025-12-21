
using PrimusSaaS.Security.Core;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PrimusSaaS.Security.Reporting;

public class PdfSecurityReporter : ISecurityReporter
{
    public PdfSecurityReporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void GenerateReport(ScanResult result, string outputPath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("Primus Security Scan Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);

                        x.Item().Text($"Scan Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                        x.Item().Text($"Duration: {result.Duration.TotalSeconds:F2} seconds");
                        x.Item().Text($"Files Scanned: {result.FilesScanned}");
                        x.Item().Text($"Status: {(result.Passed ? "PASSED" : "FAILED")}")
                            .SemiBold().FontColor(result.Passed ? Colors.Green.Medium : Colors.Red.Medium);

                        x.Item().Text("Summary of Findings").FontSize(16).SemiBold();
                        
                        // Summary Table
                        x.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Severity");
                                header.Cell().Element(CellStyle).Text("Count");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            var summaries = result.Findings
                                .GroupBy(f => f.Severity)
                                .OrderByDescending(g => g.Key);

                            foreach (var group in summaries)
                            {
                                table.Cell().Element(CellStyle).Text(group.Key.ToString());
                                table.Cell().Element(CellStyle).Text(group.Count().ToString());

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                }
                            }
                        });


                        x.Item().Text("Detailed Findings").FontSize(16).SemiBold();

                        foreach (var finding in result.Findings.OrderByDescending(f => f.Severity))
                        {
                            x.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                            {
                                c.Item().Row(row =>
                                {
                                    row.RelativeItem().Text(finding.Title).SemiBold();
                                    row.ConstantItem(100).Text(finding.Severity.ToString()).FontColor(GetSeverityColor(finding.Severity));
                                });
                                
                                c.Item().Text($"Rule: {finding.RuleId}").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().Text(finding.Description);
                                c.Item().Text($"Location: {finding.FilePath}:{finding.Line}");
                                if (!string.IsNullOrEmpty(finding.Remediation))
                                {
                                    c.Item().Text($"Remediation: {finding.Remediation}").Italic();
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        })
        .GeneratePdf(outputPath);
    }

    private string GetSeverityColor(SecuritySeverity severity)
    {
        return severity switch
        {
            SecuritySeverity.Critical => Colors.Red.Medium,
            SecuritySeverity.High => Colors.Orange.Medium,
            SecuritySeverity.Medium => Colors.Yellow.Darken2,
            _ => Colors.Blue.Medium
        };
    }
}

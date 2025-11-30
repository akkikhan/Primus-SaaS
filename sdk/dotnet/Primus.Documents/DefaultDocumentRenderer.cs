using Markdig;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Primus.Documents;

/// <summary>
/// Default implementation of IDocumentRenderer using QuestPDF.
/// </summary>
internal sealed class DefaultDocumentRenderer : IDocumentRenderer
{
    private readonly ILogger<DefaultDocumentRenderer> _logger;
    private readonly DocumentRendererOptions _options;
    private readonly MarkdownPipeline _markdownPipeline;

    public DefaultDocumentRenderer(
        ILogger<DefaultDocumentRenderer> logger,
        IOptions<DocumentRendererOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        // Configure QuestPDF license (Community license)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> RenderPdfAsync(RenderDocumentRequest request, CancellationToken ct = default)
    {
        var result = await RenderPdfWithResultAsync(request, ct);
        
        if (!result.Success)
        {
            throw new DocumentRenderException(result.Error ?? "Unknown rendering error", request.TenantId, request.ContentType);
        }

        return result.PdfBytes!;
    }

    public async Task<RenderDocumentResult> RenderPdfWithResultAsync(RenderDocumentRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var validation = ValidateRequest(request);
            if (!validation.IsValid)
            {
                LogRenderOperation(request, "ValidationFailed", null);
                return RenderDocumentResult.Fail(
                    string.Join("; ", validation.Errors),
                    request,
                    (int)sw.ElapsedMilliseconds);
            }

            // Process content based on type
            var processedContent = ProcessContent(request);

            // Generate PDF
            var pdfBytes = await Task.Run(() => GeneratePdf(request, processedContent), ct);

            LogRenderOperation(request, "Success", pdfBytes.Length);

            sw.Stop();
            return RenderDocumentResult.Ok(pdfBytes, request, (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            LogRenderOperation(request, "Error", null, ex);
            sw.Stop();
            return RenderDocumentResult.Fail(
                $"Rendering failed: {ex.Message}",
                request,
                (int)sw.ElapsedMilliseconds);
        }
    }

    public ValidationResult ValidateRequest(RenderDocumentRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            errors.Add("TenantId is required");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Title is required");
        }

        if (string.IsNullOrEmpty(request.Content))
        {
            errors.Add("Content is required");
        }
        else if (request.Content.Length > _options.MaxContentLength)
        {
            errors.Add($"Content exceeds maximum length of {_options.MaxContentLength} characters (actual: {request.Content.Length})");
        }

        if (!Enum.IsDefined(typeof(DocumentContentType), request.ContentType))
        {
            errors.Add($"Invalid content type: {request.ContentType}");
        }

        return errors.Count > 0 
            ? ValidationResult.Invalid(errors.ToArray()) 
            : ValidationResult.Valid();
    }

    private string ProcessContent(RenderDocumentRequest request)
    {
        return request.ContentType switch
        {
            DocumentContentType.PlainText => request.Content,
            DocumentContentType.Markdown => ConvertMarkdownToPlainText(request.Content),
            DocumentContentType.Html => StripHtmlToPlainText(request.Content),
            _ => request.Content
        };
    }

    private string ConvertMarkdownToPlainText(string markdown)
    {
        // Convert markdown to HTML, then strip tags for plain text rendering
        var html = Markdown.ToHtml(markdown, _markdownPipeline);
        return StripHtmlToPlainText(html);
    }

    private static string StripHtmlToPlainText(string html)
    {
        // Remove HTML tags
        var text = Regex.Replace(html, "<[^>]+>", "");
        // Decode common HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);
        // Normalize whitespace
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    private byte[] GeneratePdf(RenderDocumentRequest request, string processedContent)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(_options.PageMargin);
                page.DefaultTextStyle(x => x.FontSize(_options.BodyFontSize));

                // Header with title
                page.Header().Column(column =>
                {
                    column.Item().Text(request.Title)
                        .FontSize(_options.TitleFontSize)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    if (!string.IsNullOrWhiteSpace(request.Subtitle))
                    {
                        column.Item().Text(request.Subtitle)
                            .FontSize(_options.TitleFontSize * 0.6f)
                            .FontColor(Colors.Grey.Darken1);
                    }

                    column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // Content - split by paragraphs for proper spacing
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(10);
                    var paragraphs = processedContent.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var para in paragraphs)
                    {
                        column.Item().Text(para.Trim());
                    }
                });

                // Footer
                page.Footer().AlignCenter().Column(column =>
                {
                    column.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        var footerParts = new List<string>();

                        if (_options.IncludeTenantInFooter)
                        {
                            footerParts.Add($"Tenant: {request.TenantId}");
                        }

                        if (_options.IncludeTimestampInFooter)
                        {
                            footerParts.Add($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                        }

                        if (!string.IsNullOrWhiteSpace(_options.BrandName))
                        {
                            footerParts.Add(_options.BrandName);
                        }

                        row.RelativeItem().AlignLeft().Text(string.Join(" | ", footerParts))
                            .FontSize(8)
                            .FontColor(Colors.Grey.Medium);

                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                            text.CurrentPageNumber()
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                            text.Span(" of ")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                            text.TotalPages()
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Logs render operation metadata only - NEVER logs Content body per security requirements.
    /// </summary>
    private void LogRenderOperation(RenderDocumentRequest request, string outcome, int? pdfSizeBytes, Exception? ex = null)
    {
        // Per copilot-rules.txt: Never log document Content body, only metadata
        var contentLength = request.Content?.Length ?? 0;

        if (ex != null)
        {
            _logger.LogError(ex,
                "Document render {Outcome}: TenantId={TenantId}, ContentType={ContentType}, ContentLength={ContentLength}, Title={Title}",
                outcome, request.TenantId, request.ContentType, contentLength, request.Title);
        }
        else
        {
            _logger.LogInformation(
                "Document render {Outcome}: TenantId={TenantId}, ContentType={ContentType}, ContentLength={ContentLength}, PdfSizeBytes={PdfSizeBytes}, Title={Title}",
                outcome, request.TenantId, request.ContentType, contentLength, pdfSizeBytes ?? 0, request.Title);
        }
    }
}

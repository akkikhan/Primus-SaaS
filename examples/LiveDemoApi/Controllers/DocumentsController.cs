using LiveDemoApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Primus.Documents;
using Primus.Documents.SelfTest;

namespace LiveDemoApi.Controllers;

[ApiController]
[Route("documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentRenderer _renderer;
    private readonly IDocumentRendererSelfTest _selfTest;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    public DocumentsController(
        IDocumentRenderer renderer,
        IDocumentRendererSelfTest selfTest,
        ILogger<DocumentsController> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _renderer = renderer;
        _selfTest = selfTest;
        _logger = logger;
        _cache = cache;
        _configuration = configuration;
    }

    [HttpPost("render")]
    public async Task<IActionResult> Render(DocumentRenderRequest request)
    {
        var renderRequest = new RenderDocumentRequest
        {
            TenantId = request.TenantId ?? "demo-tenant",
            Title = request.Title ?? "Untitled Document",
            Subtitle = request.Subtitle,
            ContentType = Enum.TryParse<DocumentContentType>(request.ContentType, true, out var ct) ? ct : DocumentContentType.PlainText,
            Content = request.Content ?? string.Empty,
            Metadata = request.Metadata
        };

        var validation = _renderer.ValidateRequest(renderRequest);
        if (!validation.IsValid)
            return BadRequest(new { success = false, errors = validation.Errors });

        var result = await _renderer.RenderPdfWithResultAsync(renderRequest);
        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        _logger.LogInformation("✅ /documents/render: Generated PDF for '{Title}'", request.Title);
        return File(result.PdfBytes!, "application/pdf", $"{request.Title ?? "document"}.pdf");
    }

    [HttpPost("render/link")]
    public async Task<IActionResult> RenderWithLink(DocumentRenderRequest request)
    {
        var renderRequest = new RenderDocumentRequest
        {
            TenantId = request.TenantId ?? "demo-tenant",
            Title = request.Title ?? "Untitled Document",
            Subtitle = request.Subtitle,
            ContentType = Enum.TryParse<DocumentContentType>(request.ContentType, true, out var ct) ? ct : DocumentContentType.PlainText,
            Content = request.Content ?? string.Empty,
            Metadata = request.Metadata
        };

        var validation = _renderer.ValidateRequest(renderRequest);
        if (!validation.IsValid)
            return BadRequest(new { success = false, errors = validation.Errors });

        var result = await _renderer.RenderPdfWithResultAsync(renderRequest);
        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        var token = Guid.NewGuid().ToString("N");
        var ttlMinutes = _configuration.GetValue("PrimusDocuments:LinkTtlMinutes", 10);
        _cache.Set(token, result.PdfBytes!, TimeSpan.FromMinutes(ttlMinutes));

        _logger.LogInformation("✅ /documents/render/link: Generated PDF link for '{Title}' (TTL {Minutes} minutes)", request.Title, ttlMinutes);
        return Ok(new
        {
            success = true,
            downloadToken = token,
            expiresInMinutes = ttlMinutes
        });
    }

    [HttpGet("download/{token}")]
    public IActionResult Download(string token)
    {
        if (_cache.TryGetValue<byte[]>(token, out var bytes))
        {
            _cache.Remove(token);
            return File(bytes, "application/pdf", "document.pdf");
        }

        return NotFound(new { error = "Download token expired or invalid." });
    }

    [HttpPost("self-test")]
    public async Task<IActionResult> SelfTest(DocumentSelfTestRequest? request)
    {
        var mode = Enum.TryParse<SelfTestMode>(request?.Mode, true, out var m) ? m : SelfTestMode.Basic;
        var result = await _selfTest.RunAsync(mode);
        _logger.LogInformation("✅ /documents/self-test: {Passed}/{Total} passed", result.PassedTests, result.TotalTests);
        return Ok(new
        {
            success = result.Success,
            summary = result.Summary,
            mode = result.Mode.ToString(),
            durationMs = result.DurationMs,
            passed = result.PassedTests,
            failed = result.FailedTests
        });
    }
}

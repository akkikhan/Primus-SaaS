---
id: document-renderer
title: Document Renderer
sidebar_position: 6
description: Generate professional PDF documents from Markdown, HTML, or plain text with no external API calls.
---

# Document Renderer Module

## 1. Module Overview

**PrimusSaaS.Documents** is a text-to-PDF document rendering module for .NET applications. It converts plain text, Markdown, and HTML content into professional PDF documents using QuestPDF—all processing happens locally with zero external API calls.

Key capabilities include:
- **Multiple Content Types**: Plain text, Markdown (via Markdig), and HTML input formats
- **Professional PDFs**: QuestPDF-powered rendering with configurable typography and layout
- **Secure Link Store**: In-memory document storage with TTL-based expiration for secure downloads
- **Built-in Self-Test**: Comprehensive diagnostic tests to verify PDF rendering capabilities
- **Multi-Tenant Safe**: Tenant isolation with TenantId tracking in all operations
- **Data Sovereignty**: All rendering happens locally—your data never leaves your infrastructure

---

## 2. NuGet Installation

```bash
dotnet add package PrimusSaaS.Documents --version 1.0.0
```

Or add to your `.csproj`:

```xml
<PackageReference Include="PrimusSaaS.Documents" Version="1.0.0" />
```

Then restore packages:

```bash
dotnet restore
```

---

## 3. Using Statement

Add these using directives at the top of your `Program.cs` and any files that use document rendering:

```csharp
using Primus.Documents;
using Primus.Documents.LinkStore;
using Primus.Documents.SelfTest;  // Optional: for self-test endpoints
```

---

## 4. Program.cs Registration

### Option A: Configuration Binding (Recommended)

```csharp
using Primus.Documents;

var builder = WebApplication.CreateBuilder(args);

// Add Document Renderer with configuration binding
builder.Services.AddPrimusDocumentRenderer(options =>
    builder.Configuration.GetSection("PrimusDocuments").Bind(options));

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

### Option B: Programmatic Configuration

```csharp
using Primus.Documents;

var builder = WebApplication.CreateBuilder(args);

// Configure inline
builder.Services.AddPrimusDocumentRenderer(options =>
{
    options.Provider = "Default";
    options.MaxContentLength = 50000;           // 50KB max input
    options.LinkTtl = TimeSpan.FromMinutes(15); // 15 min download links
    options.SelfTestEnabled = true;
    
    // Layout options
    options.DefaultTitle = "Report";
    options.BrandName = "ACME Corp";
    options.PageMargin = 50f;
    options.BodyFontSize = 12f;
    options.TitleFontSize = 24f;
    options.IncludeTimestampInFooter = true;
    options.IncludeTenantInFooter = true;
});

var app = builder.Build();
app.Run();
```

### Option C: Using IConfiguration Section

```csharp
builder.Services.AddPrimusDocumentRenderer(
    builder.Configuration,
    sectionName: "PrimusDocuments");  // Default section name
```

---

## 5. appsettings.json Configuration

### Full Configuration Schema

```json
{
  "PrimusDocuments": {
    "Provider": "Default",
    "TempStoragePath": null,
    "MaxContentLength": 20000,
    "LinkTtl": "00:10:00",
    "SelfTestEnabled": true,
    "DefaultTitle": "Document",
    "BrandName": "ACME Corp",
    "IncludeTimestampInFooter": true,
    "IncludeTenantInFooter": true,
    "PageMargin": 50,
    "BodyFontSize": 12,
    "TitleFontSize": 24
  }
}
```

### Configuration Properties Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Provider` | string | `"Default"` | PDF renderer provider (QuestPDF-based) |
| `TempStoragePath` | string | `null` | Path for temp files (null = in-memory only) |
| `MaxContentLength` | int | `20000` | Max input content length in characters |
| `LinkTtl` | TimeSpan | `00:10:00` | Time-to-live for download links |
| `SelfTestEnabled` | bool | `true` | Enable self-test diagnostics (disable in prod) |
| `DefaultTitle` | string | `"Document"` | Default document title when none provided |
| `BrandName` | string | `null` | Brand name to display in footer |
| `IncludeTimestampInFooter` | bool | `true` | Include generation timestamp in footer |
| `IncludeTenantInFooter` | bool | `true` | Include tenant ID in footer |
| `PageMargin` | float | `50` | Page margin in points |
| `BodyFontSize` | float | `12` | Body text font size in points |
| `TitleFontSize` | float | `24` | Title font size in points |

### Environment Variables

```bash
PRIMUSDOCUMENTS__PROVIDER=Default
PRIMUSDOCUMENTS__MAXCONTENTLENGTH=20000
PRIMUSDOCUMENTS__LINKTTL=00:10:00
PRIMUSDOCUMENTS__SELFTESTENABLED=true
PRIMUSDOCUMENTS__BRANDNAME=ACME Corp
```

---

## 6. Middleware Pipeline

Document Renderer is **a service-based module** and does not require middleware registration. It registers `IDocumentRenderer`, `IDocumentLinkStore`, and `IDocumentRendererSelfTest` as singletons.

**Recommended endpoint setup:**

```csharp
var app = builder.Build();

// Standard middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();

// Optional: Authentication for protected documents
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

> **Note:** Keep self-test endpoints behind authentication in production or disable via `SelfTestEnabled = false`.

---

## 7. Dependencies

PrimusSaaS.Documents has the following dependencies:

| Dependency | Version | Purpose |
|------------|---------|---------|
| `QuestPDF` | 2024.3.0 | PDF generation engine |
| `Markdig` | 0.34.0 | Markdown parsing |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 7.0.0 | DI integration |
| `Microsoft.Extensions.Options` | 7.0.0 | Options pattern |
| `Microsoft.Extensions.Logging.Abstractions` | 7.0.0 | Logging integration |

> **QuestPDF License Note:** QuestPDF is open source under the MIT license. Review [QuestPDF licensing](https://www.questpdf.com/license.html) for commercial use requirements.

---

## 8. External Guides

### QuestPDF Documentation
- [QuestPDF Getting Started](https://www.questpdf.com/getting-started.html)
- [QuestPDF Document Structure](https://www.questpdf.com/quick-start.html)

### Markdown Rendering
- [Markdig Documentation](https://github.com/xoofx/markdig)
- [CommonMark Spec](https://spec.commonmark.org/)

### Microsoft Documentation
- [Options Pattern in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- [ASP.NET Core File Results](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads)

---

## 9. End-to-End Working Example

### Complete Program.cs

```csharp
using Primus.Documents;
using Primus.Documents.LinkStore;
using Primus.Documents.SelfTest;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// STEP 1: Configure Document Renderer
// ==============================================
builder.Services.AddPrimusDocumentRenderer(options =>
    builder.Configuration.GetSection("PrimusDocuments").Bind(options));

builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();

// ==============================================
// STEP 2: Document Rendering Endpoint
// ==============================================
app.MapPost("/api/documents/render", async (
    [FromBody] RenderRequest request,
    IDocumentRenderer renderer,
    IDocumentLinkStore linkStore) =>
{
    // Create render request
    var renderRequest = new RenderDocumentRequest
    {
        TenantId = request.TenantId,
        Title = request.Title,
        Subtitle = request.Subtitle,
        ContentType = request.ContentType,
        Content = request.Content
    };

    // Validate before rendering
    var validation = renderer.ValidateRequest(renderRequest);
    if (!validation.IsValid)
    {
        return Results.BadRequest(new { 
            errors = validation.Errors 
        });
    }

    // Render PDF
    var result = await renderer.RenderPdfWithResultAsync(renderRequest);

    if (!result.Success)
    {
        return Results.BadRequest(new { 
            error = result.Error 
        });
    }

    // Store for download with expiring link
    var ttl = TimeSpan.FromMinutes(10);
    var token = await linkStore.StoreAsync(
        result.TenantId,
        result.Title,
        result.PdfBytes!,
        ttl);

    return Results.Ok(new
    {
        success = true,
        downloadToken = token,
        downloadUrl = $"/api/documents/download/{token}",
        pdfSizeBytes = result.PdfSizeBytes,
        contentLength = result.ContentLength,
        durationMs = result.DurationMs,
        expiresInMinutes = ttl.TotalMinutes
    });
});

// ==============================================
// STEP 3: Download Endpoint
// ==============================================
app.MapGet("/api/documents/download/{token}", async (
    string token,
    IDocumentLinkStore linkStore) =>
{
    var doc = await linkStore.RetrieveAsync(token);
    
    if (doc == null)
    {
        return Results.NotFound(new { 
            error = "Document not found or expired" 
        });
    }

    // Return PDF file
    var filename = $"{SanitizeFilename(doc.Title)}.pdf";
    return Results.File(
        doc.PdfBytes, 
        "application/pdf", 
        filename);
});

// ==============================================
// STEP 4: Self-Test Endpoint (Development Only)
// ==============================================
app.MapPost("/api/documents/self-test", async (
    [FromBody] SelfTestRequest? request,
    IDocumentRendererSelfTest selfTest) =>
{
    var mode = request?.Mode ?? SelfTestMode.Basic;
    var result = await selfTest.RunAsync(mode);
    return Results.Ok(result);
});

// ==============================================
// STEP 5: Direct Render Endpoint (Returns PDF)
// ==============================================
app.MapPost("/api/documents/render-direct", async (
    [FromBody] RenderRequest request,
    IDocumentRenderer renderer) =>
{
    var renderRequest = new RenderDocumentRequest
    {
        TenantId = request.TenantId,
        Title = request.Title,
        ContentType = request.ContentType,
        Content = request.Content
    };

    try
    {
        var pdfBytes = await renderer.RenderPdfAsync(renderRequest);
        var filename = $"{SanitizeFilename(request.Title)}.pdf";
        return Results.File(pdfBytes, "application/pdf", filename);
    }
    catch (DocumentValidationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (DocumentRenderException ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapControllers();
app.Run();

// Helper: Sanitize filename
static string SanitizeFilename(string title) => 
    string.Join("_", title.Split(Path.GetInvalidFileNameChars()));

// Request DTOs
record RenderRequest(
    string TenantId,
    string Title,
    string? Subtitle,
    DocumentContentType ContentType,
    string Content);

record SelfTestRequest(SelfTestMode Mode = SelfTestMode.Basic);
```

### Example Controller for Document Generation

```csharp
using Microsoft.AspNetCore.Mvc;
using Primus.Documents;
using Primus.Documents.LinkStore;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IDocumentRenderer _renderer;
    private readonly IDocumentLinkStore _linkStore;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IDocumentRenderer renderer,
        IDocumentLinkStore linkStore,
        ILogger<ReportsController> logger)
    {
        _renderer = renderer;
        _linkStore = linkStore;
        _logger = logger;
    }

    [HttpPost("monthly-report")]
    public async Task<IActionResult> GenerateMonthlyReport(
        [FromQuery] string tenantId,
        [FromQuery] int month,
        [FromQuery] int year)
    {
        // Generate markdown report
        var markdown = $@"
# Monthly Report - {month:D2}/{year}

## Executive Summary
This report covers performance metrics for **{new DateTime(year, month, 1):MMMM yyyy}**.

## Key Metrics

| Metric | Value | Change |
|--------|-------|--------|
| Revenue | $125,432 | +12% |
| Active Users | 5,432 | +8% |
| Retention Rate | 94% | +2% |

## Highlights

- Successfully launched new dashboard feature
- Customer satisfaction score improved to 4.8/5
- Infrastructure costs reduced by 15%

## Next Steps

1. Continue monitoring user engagement
2. Roll out beta features to select customers
3. Schedule quarterly review meeting

---
*Generated automatically by Primus Document Renderer*
";

        var request = new RenderDocumentRequest
        {
            TenantId = tenantId,
            Title = $"Monthly Report - {month:D2}/{year}",
            Subtitle = $"Generated on {DateTime.UtcNow:yyyy-MM-dd}",
            ContentType = DocumentContentType.Markdown,
            Content = markdown
        };

        var result = await _renderer.RenderPdfWithResultAsync(request);

        if (!result.Success)
        {
            _logger.LogError("Report generation failed: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        // Store for download
        var token = await _linkStore.StoreAsync(
            result.TenantId,
            result.Title,
            result.PdfBytes!,
            TimeSpan.FromMinutes(30));

        _logger.LogInformation(
            "Report generated for tenant {TenantId}: {SizeKB}KB in {DurationMs}ms",
            tenantId,
            result.PdfSizeBytes / 1024,
            result.DurationMs);

        return Ok(new
        {
            success = true,
            downloadToken = token,
            downloadUrl = $"/api/documents/download/{token}",
            pdfSizeBytes = result.PdfSizeBytes,
            expiresAt = DateTime.UtcNow.AddMinutes(30)
        });
    }

    [HttpPost("html-invoice")]
    public async Task<IActionResult> GenerateHtmlInvoice(
        [FromBody] InvoiceRequest invoice)
    {
        var html = $@"
<h1>Invoice #{invoice.InvoiceNumber}</h1>
<p><strong>Date:</strong> {invoice.Date:yyyy-MM-dd}</p>
<p><strong>Customer:</strong> {invoice.CustomerName}</p>

<table border='1' cellpadding='8'>
    <tr>
        <th>Item</th>
        <th>Quantity</th>
        <th>Price</th>
        <th>Total</th>
    </tr>
    {string.Join("", invoice.Items.Select(i => $@"
    <tr>
        <td>{i.Name}</td>
        <td>{i.Quantity}</td>
        <td>${i.Price:F2}</td>
        <td>${i.Quantity * i.Price:F2}</td>
    </tr>"))}
</table>

<p><strong>Total: ${invoice.Items.Sum(i => i.Quantity * i.Price):F2}</strong></p>
";

        var pdfBytes = await _renderer.RenderPdfAsync(new RenderDocumentRequest
        {
            TenantId = invoice.TenantId,
            Title = $"Invoice {invoice.InvoiceNumber}",
            ContentType = DocumentContentType.Html,
            Content = html
        });

        return File(pdfBytes, "application/pdf", $"invoice_{invoice.InvoiceNumber}.pdf");
    }
}

// Invoice DTOs
public record InvoiceRequest(
    string TenantId,
    string InvoiceNumber,
    DateTime Date,
    string CustomerName,
    List<InvoiceItem> Items);

public record InvoiceItem(
    string Name,
    int Quantity,
    decimal Price);
```

### appsettings.json for the Example

```json
{
  "PrimusDocuments": {
    "Provider": "Default",
    "MaxContentLength": 50000,
    "LinkTtl": "00:30:00",
    "SelfTestEnabled": true,
    "BrandName": "ACME Corp",
    "IncludeTimestampInFooter": true,
    "IncludeTenantInFooter": true,
    "TitleFontSize": 28,
    "BodyFontSize": 11,
    "PageMargin": 40
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Primus.Documents": "Debug"
    }
  }
}
```

### Test with cURL

```bash
# Render a plain text document
curl -X POST http://localhost:5000/api/documents/render \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "tenant-123",
    "title": "My Document",
    "contentType": 0,
    "content": "Hello, World! This is a plain text document."
  }'

# Render Markdown (contentType: 1)
curl -X POST http://localhost:5000/api/documents/render \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "tenant-123",
    "title": "Markdown Report",
    "contentType": 1,
    "content": "# Hello\\n\\nThis is **bold** and *italic* text."
  }'

# Download generated PDF
curl -o document.pdf http://localhost:5000/api/documents/download/{token}

# Run self-tests
curl -X POST http://localhost:5000/api/documents/self-test \
  -H "Content-Type: application/json" \
  -d '{"mode": 3}'  # Full test mode

# Generate report directly (returns PDF)
curl -X POST http://localhost:5000/api/reports/monthly-report?tenantId=tenant-123&month=1&year=2024
```

---

## 10. Troubleshooting Section

### Issue: PDF Generation Fails with Empty Content

**Symptoms:** `RenderPdfAsync` throws `DocumentValidationException`.

**Solution:**
```csharp
// Validate before rendering
var validation = renderer.ValidateRequest(request);
if (!validation.IsValid)
{
    Console.WriteLine($"Validation errors: {string.Join(", ", validation.Errors)}");
}
```

### Issue: Content Exceeds Maximum Length

**Error:** `Content exceeds maximum length of 20000 characters`

**Solutions:**

1. **Increase limit:**
   ```json
   {
     "PrimusDocuments": {
       "MaxContentLength": 100000
     }
   }
   ```

2. **Split large documents:**
   ```csharp
   // Split content into chunks and generate multiple PDFs
   var chunks = SplitContent(content, maxLength: 20000);
   foreach (var chunk in chunks)
   {
       await renderer.RenderPdfAsync(new RenderDocumentRequest { Content = chunk });
   }
   ```

### Issue: Download Link Expired

**Symptoms:** `/download/{token}` returns 404.

**Solutions:**

1. **Increase TTL:**
   ```json
   {
     "PrimusDocuments": {
       "LinkTtl": "01:00:00"
     }
   }
   ```

2. **Check token immediately after generation:**
   ```csharp
   var result = await renderer.RenderPdfWithResultAsync(request);
   var token = await linkStore.StoreAsync(/*...*/);
   
   // Return both token and direct download
   return Ok(new { 
       downloadToken = token,
       // Or return PDF bytes directly
       pdfBase64 = Convert.ToBase64String(result.PdfBytes)
   });
   ```

### Issue: Markdown Tables Not Rendering

**Symptoms:** Tables appear as plain text.

**Solution:** Use standard Markdown table syntax:

```markdown
| Column 1 | Column 2 | Column 3 |
|----------|----------|----------|
| Cell 1   | Cell 2   | Cell 3   |
```

### Issue: QuestPDF License Warning

**Warning:** QuestPDF requires license for commercial use (revenue > $1M).

**Solution:** 
```csharp
// Add at app startup if you have a commercial license
QuestPDF.Settings.License = LicenseType.Community;  // or Professional
```

### Enable Debug Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Primus.Documents": "Debug"
    }
  }
}
```

### Run Self-Test Diagnostics

```csharp
// Full diagnostic test
var result = await selfTest.RunAsync(SelfTestMode.Full);
Console.WriteLine($"Tests passed: {result.PassedTests}/{result.TotalTests}");

foreach (var test in result.TestCases.Where(t => !t.Passed))
{
    Console.WriteLine($"FAILED: {test.Name} - {test.Details}");
}
```

---

## 11. FAQ

### Q: Can I customize the PDF layout?

**A:** Yes, use the options:

```json
{
  "PrimusDocuments": {
    "PageMargin": 40,
    "TitleFontSize": 28,
    "BodyFontSize": 11,
    "BrandName": "ACME Corp"
  }
}
```

For advanced customization, implement a custom `IDocumentRenderer`.

### Q: How do I add images to PDFs?

**A:** Use HTML content type with base64 images:

```csharp
var html = @"
<h1>Report with Image</h1>
<img src='data:image/png;base64,iVBORw0KGgo...' width='200' />
";

await renderer.RenderPdfAsync(new RenderDocumentRequest
{
    ContentType = DocumentContentType.Html,
    Content = html
});
```

### Q: Can I use custom fonts?

**A:** QuestPDF supports custom fonts. Currently, the default renderer uses system fonts. For custom fonts, implement a custom `IDocumentRenderer`.

### Q: Is the in-memory link store production-ready?

**A:** For single-instance deployments, yes. For multi-instance/load-balanced deployments, implement a custom `IDocumentLinkStore` using Redis or a database.

### Q: What's the maximum PDF size?

**A:** Depends on `MaxContentLength` (default 20K chars) and content complexity. Typical PDFs are 10-500KB. For large documents, consider pagination.

### Q: How do I add headers/footers?

**A:** The default renderer adds configurable footers. For custom headers, implement a custom `IDocumentRenderer` using QuestPDF's fluent API.

### Q: Can I password-protect PDFs?

**A:** Not built-in. Use a post-processing library like [iTextSharp](https://github.com/itext/itext7-dotnet) to add encryption.

### Q: What content types are supported?

**A:** Three types via `DocumentContentType` enum:

| Type | Enum Value | Use Case |
|------|------------|----------|
| `PlainText` | 0 | Simple reports, logs |
| `Markdown` | 1 | Documentation, READMEs |
| `Html` | 2 | Rich formatted documents |

---

## 12. Version Compatibility

| PrimusSaaS.Documents | .NET 6 | .NET 7 | .NET 8 | .NET 9 |
|---------------------|--------|--------|--------|--------|
| 1.0.0               | ✅     | ✅     | ✅     | ❌     |

### Dependencies Matrix

| Documents Version | QuestPDF | Markdig | Min ASP.NET Core |
|-------------------|----------|---------|------------------|
| 1.0.0             | 2024.3.0 | 0.34.0  | 6.0              |

---

## 13. Next Module Suggestions

After configuring Document Renderer, consider adding:

| Module | Purpose | Link |
|--------|---------|------|
| **Identity Validator** | Secure document endpoints with JWT auth | [Identity Validator →](/docs/modules/identity-validator) |
| **Notifications** | Email generated PDFs as attachments | [Notifications →](/docs/modules/notifications) |
| **Logging** | Track document generation with structured logs | [Logging Module →](/docs/modules/logging-module) |

### Integration Example: Documents + Notifications

```csharp
// Generate and email a PDF report
var pdfResult = await _renderer.RenderPdfWithResultAsync(reportRequest);

if (pdfResult.Success)
{
    await _notifications.SendEmailAsync(new EmailRequest
    {
        To = "user@example.com",
        Subject = "Your Monthly Report",
        Body = "Please find your report attached.",
        Attachments = new[] { 
            new EmailAttachment("report.pdf", pdfResult.PdfBytes!) 
        }
    });
}
```

---

## Further Reading

- [Live Demo Integration](/docs/modules/live-demo-api) — See Document Renderer in action
- [Version Matrix](/docs/modules/version-matrix) — All Primus module versions
- [QuestPDF Examples](https://www.questpdf.com/quick-start.html) — Advanced PDF layouts

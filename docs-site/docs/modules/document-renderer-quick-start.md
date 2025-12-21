---
id: document-renderer-quick-start
title: Document Renderer - Quick Start
sidebar_position: 50
description: 5-minute setup for generating PDFs from text (Markdown/HTML inputs are converted to plain text).
---

# Document Renderer Quick Start

Generate PDFs from text inputs; Markdown and HTML are converted to plain text.

:::info Complete Data Isolation
Primus Document Renderer runs **entirely within your application**. All document generation happens locally. No document content or generated files are ever transmitted to Primus servers.
:::

---

## Install

```bash
dotnet add package PrimusSaaS.Documents
```

---

## Setup (3 Lines)

```csharp
using Primus.Documents;

var builder = WebApplication.CreateBuilder(args);

// Add this ONE line
builder.Services.AddPrimusDocumentRenderer(opts => 
    builder.Configuration.GetSection("PrimusDocuments").Bind(opts));

builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers();
app.Run();
```

---

## Configure

### appsettings.json

```json
{
  "PrimusDocuments": {
    "Provider": "Default",
    "MaxContentLength": 20000,
    "LinkTtl": "00:10:00",
    "SelfTestEnabled": false,
    "BrandName": "Your Company",
    "IncludeTimestampInFooter": true,
    "IncludeTenantInFooter": true,
    "PageMargin": 50,
    "BodyFontSize": 12,
    "TitleFontSize": 24
  }
}
```

---

## Generate PDF from HTML (rendered as plain text)

```csharp
using Primus.Documents;

public class InvoiceController : ControllerBase
{
    private readonly IDocumentRenderer _renderer;

    public InvoiceController(IDocumentRenderer renderer)
    {
        _renderer = renderer;
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(int id)
    {
        var request = new RenderDocumentRequest
        {
            TenantId = "demo-tenant",
            Title = $"Invoice #{id}",
            ContentType = DocumentContentType.Html,
            Content = $"""
                <h1>Invoice #{id}</h1>
                <p>Amount: $100.00</p>
                <p>Date: {DateTime.UtcNow:yyyy-MM-dd}</p>
            """
        };

        var pdf = await _renderer.RenderPdfAsync(request);
        return File(pdf, "application/pdf", $"invoice-{id}.pdf");
    }
}
```

---

## Generate PDF from Markdown (rendered as plain text)

```csharp
[HttpGet("report")]
public async Task<IActionResult> GetReport()
{
    var request = new RenderDocumentRequest
    {
        TenantId = "demo-tenant",
        Title = "Monthly Report",
        ContentType = DocumentContentType.Markdown,
        Content = """
        # Monthly Report

        ## Summary
        - Total Sales: $10,000
        - New Customers: 50

        ## Details
        | Month | Sales |
        |-------|-------|
        | Jan   | $5,000 |
        | Feb   | $5,000 |
        """
    };

    var pdf = await _renderer.RenderPdfAsync(request);
    return File(pdf, "application/pdf", "report.pdf");
}
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| All options | [Document Renderer Reference ->](/docs/modules/document-renderer) |
| Link TTLs & temp storage | [Document Renderer Reference ->](/docs/modules/document-renderer#configuration-reference-primusdocuments) |
| Self-test endpoint | [Document Renderer Reference ->](/docs/modules/document-renderer#self-test) |


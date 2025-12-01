---
id: document-renderer-quick-start
title: Document Renderer - Quick Start
sidebar_position: 50
description: 5-minute setup for generating PDFs from HTML/Markdown.
---

# Document Renderer Quick Start

Generate PDFs from HTML and Markdown in under 5 minutes.

---

## Install

```bash
dotnet add package Primus.Documents
```

---

## Setup (3 Lines)

```csharp
using Primus.Documents;

var builder = WebApplication.CreateBuilder(args);

// Add this ONE line
builder.Services.AddPrimusDocumentRenderer(opts => 
    builder.Configuration.GetSection("PrimusDocuments").Bind(opts));

var app = builder.Build();
app.Run();
```

---

## Configure

### appsettings.json

```json
{
  "PrimusDocuments": {
    "DefaultFormat": "PDF",
    "TemplatePath": "DocumentTemplates"
  }
}
```

---

## Generate PDF from HTML

```csharp
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
        var html = $@"
            <html>
            <body>
                <h1>Invoice #{id}</h1>
                <p>Amount: $100.00</p>
                <p>Date: {DateTime.Now:yyyy-MM-dd}</p>
            </body>
            </html>";

        var pdf = await _renderer.RenderHtmlToPdfAsync(html);
        
        return File(pdf, "application/pdf", $"invoice-{id}.pdf");
    }
}
```

---

## Generate PDF from Markdown

```csharp
[HttpGet("report")]
public async Task<IActionResult> GetReport()
{
    var markdown = @"
# Monthly Report

## Summary
- Total Sales: $10,000
- New Customers: 50

## Details
| Month | Sales |
|-------|-------|
| Jan   | $5,000 |
| Feb   | $5,000 |
";

    var pdf = await _renderer.RenderMarkdownToPdfAsync(markdown);
    
    return File(pdf, "application/pdf", "report.pdf");
}
```

---

## Use Template File

### DocumentTemplates/invoice.html

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .header { background: #4F46E5; color: white; padding: 20px; }
        table { width: 100%; border-collapse: collapse; }
        th, td { border: 1px solid #ddd; padding: 8px; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Invoice #{{InvoiceNumber}}</h1>
    </div>
    <p>Customer: {{CustomerName}}</p>
    <p>Date: {{Date}}</p>
    <table>
        <tr><th>Item</th><th>Amount</th></tr>
        {{#Items}}
        <tr><td>{{Name}}</td><td>{{Price}}</td></tr>
        {{/Items}}
    </table>
    <p><strong>Total: {{Total}}</strong></p>
</body>
</html>
```

### Generate from Template

```csharp
var pdf = await _renderer.RenderTemplateAsync("invoice.html", new
{
    InvoiceNumber = "INV-001",
    CustomerName = "Acme Corp",
    Date = DateTime.Now.ToString("yyyy-MM-dd"),
    Items = new[]
    {
        new { Name = "Widget", Price = "$50.00" },
        new { Name = "Gadget", Price = "$75.00" }
    },
    Total = "$125.00"
});
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Custom styling | [Advanced Features →](/docs/modules/document-renderer-advanced) |
| Page headers/footers | [Advanced Features →](/docs/modules/document-renderer-advanced) |
| Batch generation | [Advanced Features →](/docs/modules/document-renderer-advanced) |
| Full reference | [Document Renderer Reference →](/docs/modules/document-renderer) |

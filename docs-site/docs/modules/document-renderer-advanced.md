---
id: document-renderer-advanced
title: Document Renderer - Advanced Features
sidebar_position: 51
description: Custom styling, headers/footers, watermarks, and batch generation.
---

# Document Renderer Advanced Features

Unlock custom styling, page headers/footers, watermarks, batch generation, and more.

---

## Page Setup

### Configure Page Size and Margins

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    PageSize = PageSize.A4,
    Orientation = Orientation.Portrait,
    Margins = new Margins
    {
        Top = "2cm",
        Bottom = "2cm",
        Left = "1.5cm",
        Right = "1.5cm"
    }
});
```

### Available Page Sizes

| Size | Dimensions |
|------|------------|
| `A4` | 210 × 297 mm |
| `Letter` | 8.5 × 11 in |
| `Legal` | 8.5 × 14 in |
| `A3` | 297 × 420 mm |
| `Custom` | Specify width/height |

### Custom Page Size

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    PageSize = PageSize.Custom,
    PageWidth = "6in",
    PageHeight = "9in"
});
```

---

## Headers and Footers

### Simple Header/Footer

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    HeaderHtml = "<div style='text-align: center; font-size: 10px;'>Company Name</div>",
    FooterHtml = "<div style='text-align: center; font-size: 10px;'>Page <span class='pageNumber'></span> of <span class='totalPages'></span></div>"
});
```

### Dynamic Header/Footer with Template

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    HeaderTemplate = @"
        <div style='width: 100%; font-size: 9px; border-bottom: 1px solid #ddd; padding: 5px;'>
            <span style='float: left;'>{{DocumentTitle}}</span>
            <span style='float: right;'>{{Date}}</span>
        </div>",
    FooterTemplate = @"
        <div style='width: 100%; font-size: 9px; border-top: 1px solid #ddd; padding: 5px;'>
            <span style='float: left;'>Confidential</span>
            <span style='float: right;'>Page <span class='pageNumber'></span></span>
        </div>",
    HeaderData = new { DocumentTitle = "Invoice Report", Date = DateTime.Now.ToString("yyyy-MM-dd") }
});
```

---

## Watermarks

### Text Watermark

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    Watermark = new WatermarkOptions
    {
        Text = "CONFIDENTIAL",
        FontSize = 48,
        Color = "#cccccc",
        Opacity = 0.3,
        Rotation = -45
    }
});
```

### Image Watermark

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    Watermark = new WatermarkOptions
    {
        ImagePath = "wwwroot/images/logo-watermark.png",
        Opacity = 0.2,
        Position = WatermarkPosition.Center
    }
});
```

---

## CSS Styling

### Embedded Styles

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        @page {
            size: A4;
            margin: 2cm;
        }
        
        body {
            font-family: 'Helvetica', Arial, sans-serif;
            line-height: 1.6;
        }
        
        .page-break {
            page-break-after: always;
        }
        
        table {
            width: 100%;
            border-collapse: collapse;
        }
        
        th {
            background: #4F46E5;
            color: white;
            padding: 10px;
        }
        
        td {
            border: 1px solid #ddd;
            padding: 8px;
        }
        
        .avoid-break {
            page-break-inside: avoid;
        }
    </style>
</head>
<body>
    <!-- Content -->
</body>
</html>
```

### External Stylesheets

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    StylesheetPaths = new[]
    {
        "wwwroot/css/pdf-styles.css",
        "wwwroot/css/invoice-theme.css"
    }
});
```

---

## Page Breaks

### Force Page Break

```html
<div class="invoice-page">
    <!-- Invoice content -->
</div>

<div class="page-break"></div>

<div class="terms-page">
    <!-- Terms and conditions -->
</div>

<style>
.page-break {
    page-break-after: always;
}
</style>
```

### Prevent Break Inside Element

```html
<div class="customer-section avoid-break">
    <h3>Customer Details</h3>
    <p>Name: John Doe</p>
    <p>Address: 123 Main St</p>
</div>

<style>
.avoid-break {
    page-break-inside: avoid;
}
</style>
```

---

## Template Engine

### Use Mustache Templates

```csharp
// DocumentTemplates/report.html
var template = @"
<h1>{{title}}</h1>
<p>Generated: {{date}}</p>

<h2>Summary</h2>
<ul>
{{#items}}
    <li>{{name}}: {{value}}</li>
{{/items}}
</ul>

{{#showChart}}
<div class='chart'>
    <!-- Chart content -->
</div>
{{/showChart}}
";

var pdf = await _renderer.RenderTemplateAsync(template, new
{
    title = "Monthly Report",
    date = DateTime.Now.ToString("MMMM yyyy"),
    items = new[]
    {
        new { name = "Sales", value = "$10,000" },
        new { name = "Expenses", value = "$7,500" },
        new { name = "Profit", value = "$2,500" }
    },
    showChart = true
});
```

### Use Liquid Templates

```csharp
builder.Services.AddPrimusDocumentRenderer(opts =>
{
    opts.TemplateEngine = TemplateEngine.Liquid;
    opts.TemplatePath = "DocumentTemplates";
});
```

```liquid
<h1>{{ title }}</h1>

{% for item in items %}
<div class="item">
    <span>{{ item.name }}</span>
    <span>{{ item.price | currency }}</span>
</div>
{% endfor %}

<p class="total">Total: {{ total | currency }}</p>
```

---

## Batch Generation

### Generate Multiple PDFs

```csharp
public async Task<List<byte[]>> GenerateInvoices(List<Invoice> invoices)
{
    var tasks = invoices.Select(async invoice =>
    {
        var html = await _renderer.RenderTemplateAsync("invoice.html", invoice);
        return await _renderer.RenderHtmlToPdfAsync(html);
    });
    
    return (await Task.WhenAll(tasks)).ToList();
}
```

### Merge Multiple PDFs

```csharp
var pdfs = new List<byte[]>
{
    await _renderer.RenderHtmlToPdfAsync(coverPage),
    await _renderer.RenderHtmlToPdfAsync(contentPages),
    await _renderer.RenderHtmlToPdfAsync(appendix)
};

var merged = await _renderer.MergePdfsAsync(pdfs);
return File(merged, "application/pdf", "complete-report.pdf");
```

---

## Table of Contents

### Auto-Generate TOC

```html
<div id="toc">
    <!-- TOC will be inserted here -->
</div>

<h1 data-toc>Introduction</h1>
<p>Content...</p>

<h1 data-toc>Chapter 1</h1>
<p>Content...</p>

<h2 data-toc>Section 1.1</h2>
<p>Content...</p>
```

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    GenerateTableOfContents = true,
    TocSelector = "[data-toc]",
    TocPlaceholder = "#toc"
});
```

---

## Charts and Graphs

### Using Chart.js

```html
<canvas id="chart" width="400" height="200"></canvas>
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
<script>
    const ctx = document.getElementById('chart').getContext('2d');
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['Jan', 'Feb', 'Mar'],
            datasets: [{
                label: 'Sales',
                data: [12, 19, 3]
            }]
        }
    });
</script>
```

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    WaitForJavaScript = true,
    JavaScriptTimeout = 5000  // 5 seconds
});
```

### Server-Side Charts

```csharp
// Generate chart as base64 image
var chartImage = GenerateChartImage(data);

var html = $@"
<h1>Sales Report</h1>
<img src='data:image/png;base64,{chartImage}' />
";
```

---

## Digital Signatures

### Add PDF Signature

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    DigitalSignature = new SignatureOptions
    {
        CertificatePath = "certificates/signing-cert.pfx",
        CertificatePassword = Configuration["Pdf:SigningPassword"],
        Reason = "Document Approval",
        Location = "New York",
        SignerName = "John Doe"
    }
});
```

---

## Password Protection

### Encrypt PDF

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    Security = new SecurityOptions
    {
        UserPassword = "user123",      // Password to open
        OwnerPassword = "owner456",    // Password for full access
        AllowPrinting = true,
        AllowCopying = false,
        AllowModifying = false
    }
});
```

---

## Async Streaming

### Stream Large PDFs

```csharp
[HttpGet("large-report")]
public async Task<IActionResult> GetLargeReport()
{
    var stream = await _renderer.RenderToStreamAsync(html, new PdfOptions
    {
        StreamOutput = true
    });
    
    return new FileStreamResult(stream, "application/pdf")
    {
        FileDownloadName = "large-report.pdf"
    };
}
```

---

## PDF/A Compliance

### Generate PDF/A (Archival)

```csharp
var pdf = await _renderer.RenderHtmlToPdfAsync(html, new PdfOptions
{
    PdfStandard = PdfStandard.PdfA1b,  // Or PdfA2b, PdfA3b
    EmbedFonts = true
});
```

---

## Complete Example

```csharp
using Primus.Documents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusDocumentRenderer(opts =>
{
    builder.Configuration.GetSection("PrimusDocuments").Bind(opts);
    opts.TemplateEngine = TemplateEngine.Liquid;
    opts.TemplatePath = "DocumentTemplates";
});

var app = builder.Build();

app.MapGet("/invoice/{id}/pdf", async (int id, IDocumentRenderer renderer) =>
{
    var invoice = await GetInvoice(id);
    
    var pdf = await renderer.RenderTemplateAsync("invoice.liquid", invoice, new PdfOptions
    {
        PageSize = PageSize.A4,
        Margins = new Margins { Top = "2cm", Bottom = "2cm", Left = "1.5cm", Right = "1.5cm" },
        HeaderHtml = $"<div style='text-align:right;font-size:9px'>Invoice #{id}</div>",
        FooterHtml = "<div style='text-align:center;font-size:9px'>Page <span class='pageNumber'></span></div>",
        Watermark = invoice.Status == "Draft" 
            ? new WatermarkOptions { Text = "DRAFT", Opacity = 0.2 } 
            : null
    });
    
    return Results.File(pdf, "application/pdf", $"invoice-{id}.pdf");
});

app.MapPost("/reports/batch", async (ReportRequest request, IDocumentRenderer renderer) =>
{
    var pdfs = new List<byte[]>();
    
    foreach (var report in request.Reports)
    {
        var html = await renderer.RenderTemplateAsync("report.liquid", report);
        pdfs.Add(await renderer.RenderHtmlToPdfAsync(html));
    }
    
    var merged = await renderer.MergePdfsAsync(pdfs);
    return Results.File(merged, "application/pdf", "batch-report.pdf");
});

app.Run();
```

### DocumentTemplates/invoice.liquid

```liquid
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .header { background: #4F46E5; color: white; padding: 20px; margin: -2cm -1.5cm 20px; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th { background: #f3f4f6; text-align: left; padding: 10px; }
        td { border-bottom: 1px solid #e5e7eb; padding: 10px; }
        .total { font-size: 1.2em; font-weight: bold; text-align: right; margin-top: 20px; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Invoice #{{ invoiceNumber }}</h1>
    </div>
    
    <p><strong>Customer:</strong> {{ customer.name }}</p>
    <p><strong>Date:</strong> {{ date | date: '%B %d, %Y' }}</p>
    
    <table>
        <tr>
            <th>Item</th>
            <th>Quantity</th>
            <th>Price</th>
            <th>Total</th>
        </tr>
        {% for item in items %}
        <tr>
            <td>{{ item.name }}</td>
            <td>{{ item.quantity }}</td>
            <td>{{ item.price | money }}</td>
            <td>{{ item.total | money }}</td>
        </tr>
        {% endfor %}
    </table>
    
    <p class="total">Total: {{ total | money }}</p>
</body>
</html>
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Basic setup | [Quick Start →](/docs/modules/document-renderer-quick-start) |
| Full reference | [Document Renderer Reference →](/docs/modules/document-renderer) |

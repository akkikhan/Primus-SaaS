# Primus.Documents

**Text-to-PDF Document Renderer SDK** for .NET applications.

[![NuGet Version](https://img.shields.io/nuget/v/PrimusSaaS.Documents.svg)](https://www.nuget.org/packages/PrimusSaaS.Documents/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Overview

Primus.Documents is a reusable .NET SDK that renders plain text, Markdown, or HTML content into professional PDF documents. It integrates seamlessly with ASP.NET Core via dependency injection and supports multi-tenant scenarios with built-in self-test capabilities.

## Features

- **Multiple Content Types**: PlainText, Markdown, and HTML input support
- **Multi-Tenant Safe**: Every operation is scoped to a tenant ID
- **Configurable Layout**: Page margins, font sizes, headers, footers
- **Self-Test Subsystem**: Built-in diagnostics with basic/validation/complexity/full modes
- **Link-Based Downloads**: Generate tokenized short-lived download links
- **Security First**: Never logs document content body, only metadata

## Installation

```bash
dotnet add package PrimusSaaS.Documents
```

## Quick Start

### 1. Register Services

```csharp
// Program.cs
builder.Services.AddPrimusDocumentRenderer(builder.Configuration, "PrimusDocuments");
```

### 2. Configure Options

```json
// appsettings.json
{
  "PrimusDocuments": {
    "Provider": "Default",
    "MaxContentLength": 20000,
    "LinkTtl": "00:10:00",
    "SelfTestEnabled": true,
    "BrandName": "My Company",
    "IncludeTimestampInFooter": true,
    "IncludeTenantInFooter": true
  }
}
```

### 3. Inject and Use

```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentRenderer _renderer;

    public DocumentController(IDocumentRenderer renderer)
    {
        _renderer = renderer;
    }

    [HttpPost("render")]
    public async Task<IActionResult> Render([FromBody] RenderDocumentRequest request)
    {
        var pdfBytes = await _renderer.RenderPdfAsync(request);
        return File(pdfBytes, "application/pdf", $"{request.Title}.pdf");
    }
}
```

## Content Types

### PlainText
```csharp
var request = new RenderDocumentRequest
{
    TenantId = "tenant-123",
    Title = "My Document",
    Content = "This is plain text content.",
    ContentType = DocumentContentType.PlainText
};
```

### Markdown
```csharp
var request = new RenderDocumentRequest
{
    TenantId = "tenant-123",
    Title = "My Document",
    Content = "# Heading\n\nThis is **bold** text.",
    ContentType = DocumentContentType.Markdown
};
```

### HTML
```csharp
var request = new RenderDocumentRequest
{
    TenantId = "tenant-123",
    Title = "My Document",
    Content = "<h1>Hello</h1><p>World</p>",
    ContentType = DocumentContentType.Html
};
```

## Self-Test

Run built-in diagnostics to verify the module is working correctly:

```csharp
var selfTest = serviceProvider.GetRequiredService<IDocumentRendererSelfTest>();
var result = await selfTest.RunAsync(SelfTestMode.Full);

Console.WriteLine($"Success: {result.Success}");
Console.WriteLine($"Passed: {result.PassedTests}/{result.TotalTests}");
```

### Test Modes

| Mode | Description |
|------|-------------|
| `Basic` | Smoke tests with minimal valid input |
| `Validation` | Tests that invalid inputs are rejected |
| `Complexity` | Tests with long content, unicode, special chars |
| `Full` | Runs all test modes |

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `Provider` | `"Default"` | Rendering provider (QuestPDF-based) |
| `TempStoragePath` | `null` | Path for temp file storage (optional) |
| `MaxContentLength` | `20000` | Maximum content length in characters |
| `LinkTtl` | `00:10:00` | TTL for download links |
| `SelfTestEnabled` | `true` | Enable self-test endpoints |
| `DefaultTitle` | `"Document"` | Default title when none provided |
| `BrandName` | `null` | Brand name in footer |
| `IncludeTimestampInFooter` | `true` | Show generation timestamp |
| `IncludeTenantInFooter` | `true` | Show tenant ID in footer |
| `PageMargin` | `50` | Page margin in points |
| `BodyFontSize` | `12` | Body text font size |
| `TitleFontSize` | `24` | Title font size |

## Link-Based Downloads

For scenarios where you need a temporary download URL instead of direct streaming:

```csharp
var linkStore = serviceProvider.GetRequiredService<IDocumentLinkStore>();

// Store PDF and get token
var token = await linkStore.StoreAsync(tenantId, title, pdfBytes, TimeSpan.FromMinutes(10));

// Later: retrieve by token
var doc = await linkStore.RetrieveAsync(token);
if (doc != null)
{
    return File(doc.PdfBytes, "application/pdf");
}
```

## Security

⚠️ **This module never logs document Content body** — only metadata:
- TenantId
- ContentType
- ContentLength
- Operation outcome
- PDF size

This ensures no sensitive document content appears in logs.

## Requirements

- .NET 6.0, 7.0, or 8.0
- QuestPDF Community License (automatically configured)

## License

MIT License - see [LICENSE](LICENSE) for details.

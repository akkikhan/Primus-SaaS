---
id: document-renderer
title: Document Renderer
sidebar_position: 6
description: Render plain text, Markdown, or HTML to PDF entirely inside your app.
---

# Document Renderer

Render plain text, Markdown, or HTML into PDF with no external services. All processing stays in your app.

---

## Install

```bash
dotnet add package PrimusSaaS.Documents
```

---

## Minimal setup

Program.cs
```csharp
using Primus.Documents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusDocumentRenderer(opts =>
    builder.Configuration.GetSection("PrimusDocuments").Bind(opts));

builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers();
app.Run();
```

appsettings.json
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

## Use it

HTML example
```csharp
var request = new RenderDocumentRequest
{
    TenantId = "demo-tenant",
    Title = "Invoice",
    ContentType = DocumentContentType.Html,
    Content = """
      <h1>Invoice</h1>
      <p>Amount: $100.00</p>
    """
};
var pdfBytes = await _renderer.RenderPdfAsync(request);
return File(pdfBytes, "application/pdf", "invoice.pdf");
```

Markdown example
```csharp
var request = new RenderDocumentRequest
{
    TenantId = "demo-tenant",
    Title = "Report",
    ContentType = DocumentContentType.Markdown,
    Content = """
    # Monthly Report
    - Total Sales: $10,000
    - New Customers: 50
    """
};
var pdf = await _renderer.RenderPdfAsync(request);
```

Plain text example
```csharp
var request = new RenderDocumentRequest
{
    TenantId = "demo-tenant",
    Title = "Note",
    ContentType = DocumentContentType.PlainText,
    Content = "Hello from Primus Documents"
};
var pdf = await _renderer.RenderPdfAsync(request);
```

---

## Configuration reference (PrimusDocuments)

| Key | Type | Default | Description |
| --- | ---- | ------- | ----------- |
| `Provider` | string | `Default` | Rendering provider (QuestPDF-based). |
| `TempStoragePath` | string | `null` | Optional temp storage path for link-based downloads; null uses memory. |
| `MaxContentLength` | int | `20000` | Max characters allowed in content. |
| `LinkTtl` | timespan | `00:10:00` | Lifetime for generated download links. |
| `SelfTestEnabled` | bool | `true` | Enable self-test; disable in production. |
| `DefaultTitle` | string | `"Document"` | Fallback title when none provided. |
| `BrandName` | string | `null` | Brand footer text. |
| `IncludeTimestampInFooter` | bool | `true` | Show generated timestamp. |
| `IncludeTenantInFooter` | bool | `true` | Show tenant id in footer. |
| `PageMargin` | float | `50` | Page margin (points). |
| `BodyFontSize` | float | `12` | Body font size. |
| `TitleFontSize` | float | `24` | Title font size. |

### Request fields

| Field | Required | Description |
| ----- | -------- | ----------- |
| `TenantId` | Yes | Tenant identifier for isolation. |
| `Title` | Yes | Document title. |
| `Subtitle` | No | Optional subtitle. |
| `ContentType` | Yes | `PlainText`, `Markdown`, or `Html`. |
| `Content` | Yes | Body to render. |
| `Metadata` | No | Optional key/value metadata for logs. |

---

## Self-test

If `SelfTestEnabled` is true, you can resolve `IDocumentRendererSelfTest` and run built-in checks:
```csharp
var selfTest = serviceProvider.GetRequiredService<Primus.Documents.SelfTest.IDocumentRendererSelfTest>();
var result = await selfTest.RunAsync(Primus.Documents.SelfTest.SelfTestMode.Basic);
```
Keep disabled in production.

---

## Notes

- Content is rendered locally; do not pass untrusted HTML without sanitization.
- Store secrets (if any future provider options) outside source control (user secrets/Key Vault).
- Max content length protects against oversized requests; tune as needed.

---
id: document-renderer
title: Document Renderer
sidebar_position: 5
---

Generate professional PDF documents from Markdown, HTML, or plain text. Primus Documents runs entirely in your application—no cloud services, no external API calls, no data leaving your infrastructure.

## Packages

- **.NET**: `Primus.Documents` (ASP.NET Core service + QuestPDF renderer)

Version source of truth: [Modules Version Matrix](/docs/modules/version-matrix).

## Highlights

- **Content Types**: Supports plain text, Markdown (with Markdig), and HTML input
- **Professional PDFs**: QuestPDF-powered document generation with proper typography and layout
- **Secure Link Store**: In-memory document storage with TTL-based expiration
- **Self-Test System**: Built-in diagnostic tests to verify PDF rendering capabilities
- **Zero External Calls**: All rendering happens locally—your data never leaves your stack

## Quick Install

```bash
# .NET / ASP.NET Core
dotnet add package Primus.Documents
```

## Quick Start

### 1. Register Services

```csharp
// Program.cs
using Primus.Documents;

var builder = WebApplication.CreateBuilder(args);

// Add Document Renderer with configuration
builder.Services.AddPrimusDocumentRenderer(options =>
    builder.Configuration.GetSection("PrimusDocuments").Bind(options));

var app = builder.Build();
```

### 2. Configuration

```json
// appsettings.json
{
  "PrimusDocuments": {
    "Provider": "QuestPDF",
    "TempStorePath": "temp/documents",
    "MaxContentLength": 20000,
    "LinkTtlMinutes": 10,
    "SelfTestEnabled": true
  }
}
```

### 3. Create an Endpoint

```csharp
using Primus.Documents;
using Primus.Documents.LinkStore;

app.MapPost("/documents/render", async (
    RenderRequest request,
    IDocumentRenderer renderer,
    IDocumentLinkStore linkStore) =>
{
    var result = await renderer.RenderAsync(new RenderDocumentRequest
    {
        TenantId = request.TenantId,
        Title = request.Title,
        Subtitle = request.Subtitle,
        ContentType = request.ContentType,
        Content = request.Content
    });

    if (!result.Success)
        return Results.BadRequest(new { error = result.Error });

    // Store with expiring link
    var token = await linkStore.StoreAsync(new StoredDocument
    {
        TenantId = result.TenantId,
        Title = result.Title,
        PdfBytes = result.PdfBytes,
        ExpiresAt = DateTime.UtcNow.AddMinutes(10)
    });

    return Results.Ok(new {
        success = true,
        downloadToken = token,
        pdfSizeBytes = result.PdfSizeBytes,
        durationMs = result.DurationMs
    });
});

record RenderRequest(
    string TenantId,
    string Title,
    string? Subtitle,
    DocumentContentType ContentType,
    string Content);
```

### 4. Download Endpoint

```csharp
app.MapGet("/documents/download/{token}", async (
    string token,
    IDocumentLinkStore linkStore) =>
{
    var doc = await linkStore.GetAsync(token);
    if (doc == null)
        return Results.NotFound(new { error = "Document not found or expired" });

    var filename = $"{doc.Title?.Replace(" ", "_") ?? "document"}.pdf";
    return Results.File(doc.PdfBytes, "application/pdf", filename);
});
```

## Content Types

The Document Renderer supports three content types:

| Type | Description | Use Case |
|------|-------------|----------|
| `PlainText` | Raw text converted to PDF | Simple reports, logs |
| `Markdown` | Markdown parsed with Markdig | Documentation, READMEs |
| `Html` | HTML rendered to PDF | Rich formatted documents |

### Markdown Example

```csharp
var result = await renderer.RenderAsync(new RenderDocumentRequest
{
    TenantId = "tenant-123",
    Title = "Monthly Report",
    ContentType = DocumentContentType.Markdown,
    Content = @"
# Monthly Report

## Summary
This report covers **January 2024** performance metrics.

### Key Highlights
- Revenue: $1.2M
- New Users: 5,432
- Retention: 94%

## Details
| Metric | Value | Change |
|--------|-------|--------|
| Revenue | $1.2M | +12% |
| Users | 5,432 | +8% |
"
});
```

## Self-Test System

The Document Renderer includes a comprehensive self-test system to verify PDF rendering capabilities:

### Test Modes

| Mode | Description | Tests Included |
|------|-------------|----------------|
| `Basic` | Quick sanity check | PlainText, Markdown rendering |
| `Validation` | Input validation tests | Empty content, max length, special chars |
| `Complexity` | Stress tests | Large documents, complex Markdown |
| `Full` | All tests | Basic + Validation + Complexity |

### Running Self-Tests

```csharp
using Primus.Documents.SelfTest;

app.MapPost("/documents/self-test", async (
    SelfTestRequest request,
    IDocumentRendererSelfTest selfTest) =>
{
    var result = await selfTest.RunAsync(request.Mode);
    return Results.Ok(result);
});

record SelfTestRequest(SelfTestMode Mode = SelfTestMode.Basic);
```

### Self-Test Response

```json
{
  "success": true,
  "mode": "Full",
  "totalTests": 12,
  "passedTests": 12,
  "failedTests": 0,
  "durationMs": 234,
  "testCases": [
    {
      "name": "PlainText_Basic",
      "category": "Basic",
      "passed": true,
      "durationMs": 45,
      "details": "Successfully rendered plain text document"
    }
  ],
  "summary": "All 12 tests passed in 234ms"
}
```

## Link Store

The Document Link Store provides temporary storage for generated PDFs with automatic expiration:

```csharp
// Store a document
var token = await linkStore.StoreAsync(new StoredDocument
{
    TenantId = "tenant-123",
    Title = "Invoice",
    PdfBytes = pdfBytes,
    ExpiresAt = DateTime.UtcNow.AddMinutes(10)
});

// Retrieve a document
var doc = await linkStore.GetAsync(token);
if (doc != null)
{
    // Document found and not expired
    var pdf = doc.PdfBytes;
}

// Delete a document
await linkStore.DeleteAsync(token);
```

### Custom Link Store

Implement `IDocumentLinkStore` for custom storage (Redis, database, etc.):

```csharp
public class RedisDocumentLinkStore : IDocumentLinkStore
{
    public Task<string> StoreAsync(StoredDocument document) { /* ... */ }
    public Task<StoredDocument?> GetAsync(string token) { /* ... */ }
    public Task DeleteAsync(string token) { /* ... */ }
}

// Register custom store
services.AddSingleton<IDocumentLinkStore, RedisDocumentLinkStore>();
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Provider` | string | `"QuestPDF"` | PDF rendering provider |
| `TempStorePath` | string | `"temp/documents"` | Temp file storage path |
| `MaxContentLength` | int | `20000` | Max input content length (chars) |
| `LinkTtlMinutes` | int | `10` | Default link expiration time |
| `SelfTestEnabled` | bool | `true` | Enable self-test endpoint |

## Environment Variables

```bash
PRIMUSDOCUMENTS__PROVIDER=QuestPDF
PRIMUSDOCUMENTS__TEMPSTOREPATH=temp/documents
PRIMUSDOCUMENTS__MAXCONTENTLENGTH=20000
PRIMUSDOCUMENTS__LINKTTLMINUTES=10
PRIMUSDOCUMENTS__SELFTESTENABLED=true
```

## Error Handling

The renderer returns structured errors in the `RenderDocumentResult`:

```csharp
var result = await renderer.RenderAsync(request);

if (!result.Success)
{
    // Handle error
    Console.WriteLine($"Render failed: {result.Error}");
    return Results.BadRequest(new { error = result.Error });
}
```

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| `Content is required` | Empty content field | Provide content |
| `Content exceeds maximum length` | Input too large | Reduce content or increase `MaxContentLength` |
| `Invalid content type` | Unsupported type | Use `PlainText`, `Markdown`, or `Html` |

## Golden Path Example

See the [LiveDemoApi](https://github.com/akkikhan/Primus-SaaS/tree/main/examples/LiveDemoApi) for a complete working example demonstrating:

1. Document rendering with all content types
2. Secure download links with TTL
3. Self-test diagnostics
4. Frontend integration

## Security Considerations

- **No external calls**: All PDF rendering happens locally
- **Tenant isolation**: TenantId is embedded in documents for audit trails
- **Expiring links**: Documents auto-expire (configurable TTL)
- **Content validation**: Input length limits prevent DoS attacks
- **No PII logging**: Document content is never logged (only metadata)

## See Also

- [Identity Validator](/docs/modules/identity-validator)
- [Logging Module](/docs/modules/logging-module)
- [Notifications](/docs/modules/notifications)

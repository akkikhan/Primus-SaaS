# Primus SaaS – Text-to-PDF / Document Renderer Module

## 1. Purpose

Provide a reusable, multi-tenant-safe **Text-to-PDF / Document Renderer** module that converts text/markdown/HTML
into a branded PDF and returns it as a download or link. The module must behave like other core Primus modules
(Identity, Logging, Notifications): SDK-first, configuration-driven, Live UI Demo integrated, and testable.

This module will also include **built-in self-tests and multi-level validation** so that engineers (and AI agents)
can verify correctness and robustness automatically.

---

## 2. Scope

### 2.1 In-Scope (v1)

- Accept content in three formats:
  - `plain` (plain text)
  - `markdown`
  - `html` (trusted/sanitized for demo)
- Convert input into a single-page or multi-page PDF using a configurable renderer.
- Basic layout: title, optional subtitle, body, optional footer with timestamp and tenant label.
- Return either:
  - Direct `application/pdf` HTTP response, or
  - Short-lived download link (for demo purposes).
- Multi-tenant safety:
  - Every call is logically scoped to a `tenantId`.
  - No cross-tenant visibility or shared content.
- Integrated logging of **metadata only**.
- Live UI Demo tile+screen in the existing Live Demo.

### 2.2 Out-of-Scope (v1)

- Long-term document storage / library
- Advanced page composition (tables, images, complex layout)
- Email attachment flows (can be added later via Notifications integration)
- Full WYSIWYG editor (rich text editing can be a future module)

---

## 3. .NET SDK Design

Namespace suggestion: `Primus.Documents` (or `Primus.PdfRenderer`).

### 3.1 Options

```csharp
public class DocumentRendererOptions
{
    public string Provider { get; set; } = "Default";
    public string? TempStoragePath { get; set; }
    public int MaxContentLength { get; set; } = 20000; // characters
    public TimeSpan LinkTtl { get; set; } = TimeSpan.FromMinutes(10);
    public bool SelfTestEnabled { get; set; } = true; // can be disabled in Production
}
```

### 3.2 Content Types & Request Model

```csharp
public enum DocumentContentType
{
    PlainText,
    Markdown,
    Html
}

public sealed class RenderDocumentRequest
{
    public string TenantId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Subtitle { get; set; }
    public DocumentContentType ContentType { get; set; }
    public string Content { get; set; } = default!;
    public IDictionary<string, string>? Metadata { get; set; }
}
```

### 3.3 Service Interface

```csharp
public interface IDocumentRenderer
{
    Task<byte[]> RenderPdfAsync(RenderDocumentRequest request, CancellationToken ct = default);
}
```

### 3.4 DI Extension

```csharp
public static class PrimusDocumentRendererServiceCollectionExtensions
{
    public static IServiceCollection AddPrimusDocumentRenderer(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.Configure<DocumentRendererOptions>(config.GetSection("PrimusDocuments"));
        services.AddScoped<IDocumentRenderer, DefaultDocumentRenderer>();

        // Optional: register a self-test runner
        services.AddScoped<IDocumentRendererSelfTest, DocumentRendererSelfTest>();

        return services;
    }
}
```

`DefaultDocumentRenderer` should use a battle-tested PDF library (e.g., QuestPDF or similar)
but remain abstracted so the provider can be swapped later.

---

## 4. HTTP API Design (LiveDemoApi)

Controller: `DocumentRendererController`

### 4.1 Endpoints

1. **Render directly**

`POST /api/documents/render`

- Request body: `RenderDocumentRequest` (for demo, include TenantId in body).
- Response: `application/pdf` file stream with `Content-Disposition: attachment; filename="document.pdf"`.

2. **Render and return link (optional)**

`POST /api/documents/render/link`

- Request body: same as above.
- Behavior:
  - Generate PDF and store in temp storage (file system or in-memory dictionary).
  - Create a tokenized short-lived link using `LinkTtl` from options.
- Response JSON:

```jsonc
{
  "downloadUrl": "https://localhost:5221/api/documents/download/{token}",
  "expiresAtUtc": "2025-01-01T00:00:00Z"
}
```

3. **Download by token (if link mode is used)**

`GET /api/documents/download/{token}`

- Validates token expiry.
- Returns `application/pdf` or `404` if not found/expired.

4. **Self-test endpoint**

`POST /api/documents/self-test`

- Enabled only when `DocumentRendererOptions.SelfTestEnabled == true`.
- Request JSON:

```jsonc
{
  "mode": "basic" | "validation" | "complexity" | "full"
}
```

- Response JSON:

```jsonc
{
  "mode": "full",
  "overallStatus": "Success" | "PartialFailure" | "Failure",
  "startedAtUtc": "2025-01-01T00:00:00Z",
  "completedAtUtc": "2025-01-01T00:00:01Z",
  "tests": [
    {
      "name": "PlainText_Short_Smoke",
      "category": "basic",
      "status": "Success",
      "durationMs": 50,
      "contentLength": 42,
      "pdfSizeBytes": 8192,
      "error": null
    },
    {
      "name": "Markdown_Long_With_Lists_And_Headers",
      "category": "complexity",
      "status": "Failure",
      "durationMs": 120,
      "contentLength": 12000,
      "pdfSizeBytes": 0,
      "error": "Exception or validation message"
    }
  ]
}
```

---

## 5. Live UI Demo Requirements

Add a new tile/section in **LiveDemoFrontend** called **“Text to PDF”**.

### 5.1 UI Features

- Text area for content.
- Dropdown for `ContentType`: Plain text / Markdown / HTML.
- Title + optional subtitle inputs.
- Optional metadata key/value editor.
- Tenant selector or read-only `demo-tenant-001` label.
- Buttons:
  - **Generate PDF** → calls `/api/documents/render` and triggers browser download.
  - **Generate PDF Link** → calls `/render/link` and displays the returned URL and expiry.
  - **Run Self-Test** → calls `/self-test` with a chosen mode and displays structured results.

### 5.2 UX for Self-Test Results

- Show a table with:
  - Test name
  - Category (basic/validation/complexity/full)
  - Status (Success/Failure)
  - Duration
  - Content length
  - Error (if any)
- Highlight failed tests clearly.

---

## 6. Configuration

Extend `.env.example` and `appsettings.*` with:

```env
# Document renderer
PRIMUSDOCUMENTS__PROVIDER=Default
PRIMUSDOCUMENTS__MAXCONTENTLENGTH=20000
PRIMUSDOCUMENTS__LINKTTL__MINUTES=10
PRIMUSDOCUMENTS__TEMPSTORAGEPATH=
PRIMUSDOCUMENTS__SELFTESTENABLED=true
```

LiveDemoApi `Program.cs` must include:

```csharp
builder.Services.AddPrimusDocumentRenderer(builder.Configuration);
```

---

## 7. Logging, Security & Privacy

- Never log the document `Content` body.
- Log only metadata:
  - `TenantId`
  - `DocumentContentType`
  - `ContentLength`
  - Operation type (`Render`, `RenderLink`, `Download`, `SelfTest`)
  - Outcome (`Success`, `ValidationError`, `RenderError`)
- Integrate with the existing Logging module following its patterns.
- Respect `copilot-rules.txt`:
  - No secrets or PII logging.
  - No tight coupling: this module can depend on Logging abstractions, but not on their internal implementations.

---

## 8. Self-Test & Multi-Level Validation (Core Requirement)

The module must include **built-in self-test capabilities** to help humans and AI agents validate behavior quickly.

### 8.1 Self-Test Runner

Create an internal service:

```csharp
public interface IDocumentRendererSelfTest
{
    Task<DocumentRendererSelfTestResult> RunAsync(string mode, CancellationToken ct = default);
}
```

Where `mode` can be:

- `basic` – minimal smoke checks only.
- `validation` – focuses on invalid inputs & boundary conditions.
- `complexity` – pushes multiple complex content types.
- `full` – runs all tests above.

The result model:

```csharp
public sealed class DocumentRendererSelfTestResult
{
    public string Mode { get; set; } = default!;
    public string OverallStatus { get; set; } = default!; // Success, PartialFailure, Failure
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
    public IReadOnlyList<DocumentRendererSelfTestCaseResult> Tests { get; set; } = Array.Empty<DocumentRendererSelfTestCaseResult>();
}

public sealed class DocumentRendererSelfTestCaseResult
{
    public string Name { get; set; } = default!;
    public string Category { get; set; } = default!; // basic, validation, complexity
    public string Status { get; set; } = default!;   // Success, Failure
    public int DurationMs { get; set; }
    public int ContentLength { get; set; }
    public int PdfSizeBytes { get; set; }
    public string? Error { get; set; }
}
```

### 8.2 Test Case Categories

**Basic (Smoke)**

- Short plain text (`"Hello Primus"`).
- Short markdown (header + list).
- Short HTML (single `<p>` element) if enabled.

**Validation**

- Empty title → expect validation failure (400 / exception with clear message).
- Content length just above `MaxContentLength` → expect `400` with explanation.
- Invalid `ContentType` enum value (if possible via API) → reject.

**Complexity**

- Long markdown (multi-section, lists, headings).
- Text with Unicode (emoji, non-Latin scripts, RTL sample).
- HTML with nested tags (basic headings, paragraphs, bold/italics).

These tests should ensure:

- PDF generation does not crash.
- Reasonable performance (capture `DurationMs`).
- Output file is non-zero size on success.

### 8.3 Environmental Controls

- `SelfTestEnabled` can be turned off in Production environments.
- If disabled, the `/self-test` endpoint must return `403` or similar with a clear message.
- Self-tests must not create persistent or unbounded temp files.

---

## 9. Automated Tests (Unit, Integration, Golden Path)

Create automated tests alongside self-test functionality.

### 9.1 Unit Tests

- `DefaultDocumentRenderer`:
  - Renders PDF for valid inputs of each `DocumentContentType`.
  - Throws or returns error for oversized content.
- `DocumentRendererSelfTest`:
  - Runs `basic` mode and returns `Success` when renderer is wired correctly.
  - When the renderer is mocked to fail, self-test should report `Failure` and propagate error messages.

### 9.2 Integration Tests (LiveDemoApi)

- `POST /api/documents/render`:
  - Returns `200` and `application/pdf` for a valid request.
  - Returns `400` for invalid/oversized input.
- `POST /api/documents/self-test`:
  - With `SelfTestEnabled = true`, returns structured result JSON with at least one test entry.
  - With `SelfTestEnabled = false`, returns `403` or similar.

### 9.3 Golden Path

Document and test a full Golden Path scenario:

1. Run LiveDemoApi (`dotnet run`).
2. Call `/api/documents/render` with a valid markdown body.
3. Receive PDF and verify basic properties (non-empty, content type).
4. (Optionally via frontend) trigger download from the Live Demo UI.

---

## 10. Definition of Done – Text-to-PDF Module

The module is considered complete when:

- SDK implemented with options, interfaces, and default renderer.
- LiveDemoApi endpoints are wired and functional.
- LiveDemoFrontend has a working “Text to PDF” screen with self-test UI.
- Configuration is documented and env-driven.
- Self-test endpoint exists and exercises multiple levels of validation (basic/validation/complexity/full).
- Unit, integration, and Golden Path tests pass in CI.
- There is no violation of `copilot-rules.txt` (no secrets, no tight coupling, no logging of document body).

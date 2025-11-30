using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Primus.Documents.SelfTest;

/// <summary>
/// Default implementation of self-test capabilities for the document renderer.
/// </summary>
internal sealed class DocumentRendererSelfTest : IDocumentRendererSelfTest
{
    private readonly IDocumentRenderer _renderer;
    private readonly ILogger<DocumentRendererSelfTest> _logger;
    private readonly DocumentRendererOptions _options;
    private const string SelfTestTenantId = "self-test-tenant";

    public DocumentRendererSelfTest(
        IDocumentRenderer renderer,
        ILogger<DocumentRendererSelfTest> logger,
        IOptions<DocumentRendererOptions> options)
    {
        _renderer = renderer;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<DocumentRendererSelfTestResult> RunAsync(SelfTestMode mode, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var testCases = new List<SelfTestCaseResult>();

        _logger.LogInformation("Starting document renderer self-test with mode: {Mode}", mode);

        try
        {
            switch (mode)
            {
                case SelfTestMode.Basic:
                    testCases.AddRange(await RunBasicTestsAsync(ct));
                    break;

                case SelfTestMode.Validation:
                    testCases.AddRange(await RunValidationTestsAsync(ct));
                    break;

                case SelfTestMode.Complexity:
                    testCases.AddRange(await RunComplexityTestsAsync(ct));
                    break;

                case SelfTestMode.Full:
                    testCases.AddRange(await RunBasicTestsAsync(ct));
                    testCases.AddRange(await RunValidationTestsAsync(ct));
                    testCases.AddRange(await RunComplexityTestsAsync(ct));
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Self-test suite failed unexpectedly");
            testCases.Add(new SelfTestCaseResult
            {
                Name = "SelfTestExecution",
                Category = "System",
                Passed = false,
                Error = $"Test suite failed: {ex.Message}"
            });
        }

        sw.Stop();

        var passed = testCases.Count(t => t.Passed);
        var failed = testCases.Count(t => !t.Passed);

        var result = new DocumentRendererSelfTestResult
        {
            Success = failed == 0,
            Mode = mode,
            TotalTests = testCases.Count,
            PassedTests = passed,
            FailedTests = failed,
            DurationMs = (int)sw.ElapsedMilliseconds,
            TestCases = testCases,
            Summary = $"Self-test {mode}: {passed}/{testCases.Count} passed in {sw.ElapsedMilliseconds}ms"
        };

        _logger.LogInformation("Self-test completed: {Summary}", result.Summary);

        return result;
    }

    private async Task<List<SelfTestCaseResult>> RunBasicTestsAsync(CancellationToken ct)
    {
        var results = new List<SelfTestCaseResult>();

        // Test 1: Basic PlainText render
        results.Add(await RunTestCaseAsync("PlainText_BasicRender", "Basic", async () =>
        {
            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Self-Test Document",
                Content = "This is a basic self-test document.",
                ContentType = DocumentContentType.PlainText
            };
            var pdf = await _renderer.RenderPdfAsync(request, ct);
            if (pdf.Length == 0) throw new Exception("PDF was empty");
            return $"Generated PDF: {pdf.Length} bytes";
        }));

        // Test 2: Markdown render
        results.Add(await RunTestCaseAsync("Markdown_BasicRender", "Basic", async () =>
        {
            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Markdown Self-Test",
                Content = "# Heading\n\nThis is **bold** and *italic* text.\n\n- Item 1\n- Item 2",
                ContentType = DocumentContentType.Markdown
            };
            var pdf = await _renderer.RenderPdfAsync(request, ct);
            if (pdf.Length == 0) throw new Exception("PDF was empty");
            return $"Generated PDF: {pdf.Length} bytes";
        }));

        // Test 3: HTML render
        results.Add(await RunTestCaseAsync("Html_BasicRender", "Basic", async () =>
        {
            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "HTML Self-Test",
                Content = "<h1>Hello</h1><p>This is <strong>HTML</strong> content.</p>",
                ContentType = DocumentContentType.Html
            };
            var pdf = await _renderer.RenderPdfAsync(request, ct);
            if (pdf.Length == 0) throw new Exception("PDF was empty");
            return $"Generated PDF: {pdf.Length} bytes";
        }));

        // Test 4: With subtitle
        results.Add(await RunTestCaseAsync("WithSubtitle_BasicRender", "Basic", async () =>
        {
            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Main Title",
                Subtitle = "This is a subtitle",
                Content = "Document with title and subtitle.",
                ContentType = DocumentContentType.PlainText
            };
            var pdf = await _renderer.RenderPdfAsync(request, ct);
            if (pdf.Length == 0) throw new Exception("PDF was empty");
            return $"Generated PDF: {pdf.Length} bytes";
        }));

        return results;
    }

    private async Task<List<SelfTestCaseResult>> RunValidationTestsAsync(CancellationToken ct)
    {
        var results = new List<SelfTestCaseResult>();

        // Test 1: Missing TenantId
        results.Add(RunSyncTestCase("MissingTenantId_ShouldFail", "Validation", () =>
        {
            var request = new RenderDocumentRequest
            {
                TenantId = "",
                Title = "Test",
                Content = "Content"
            };
            var validation = _renderer.ValidateRequest(request);
            if (validation.IsValid) throw new Exception("Should have failed validation for missing TenantId");
            return $"Correctly rejected: {string.Join(", ", validation.Errors)}";
        }));

        // Test 2: Missing Title
        results.Add(RunSyncTestCase("MissingTitle_ShouldFail", "Validation", () =>
        {
            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "",
                Content = "Content"
            };
            var validation = _renderer.ValidateRequest(request);
            if (validation.IsValid) throw new Exception("Should have failed validation for missing Title");
            return $"Correctly rejected: {string.Join(", ", validation.Errors)}";
        }));

        // Test 3: Missing Content
        results.Add(RunSyncTestCase("MissingContent_ShouldFail", "Validation", () =>
        {
            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Test",
                Content = ""
            };
            var validation = _renderer.ValidateRequest(request);
            if (validation.IsValid) throw new Exception("Should have failed validation for missing Content");
            return $"Correctly rejected: {string.Join(", ", validation.Errors)}";
        }));

        // Test 4: Content exceeds max length
        results.Add(RunSyncTestCase("ContentTooLong_ShouldFail", "Validation", () =>
        {
            var longContent = new string('x', _options.MaxContentLength + 1);
            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Test",
                Content = longContent
            };
            var validation = _renderer.ValidateRequest(request);
            if (validation.IsValid) throw new Exception("Should have failed validation for content too long");
            return $"Correctly rejected: {string.Join(", ", validation.Errors)}";
        }));

        // Test 5: Content at exactly max length (should pass)
        results.Add(RunSyncTestCase("ContentAtMaxLength_ShouldPass", "Validation", () =>
        {
            var maxContent = new string('x', _options.MaxContentLength);
            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Test",
                Content = maxContent
            };
            var validation = _renderer.ValidateRequest(request);
            if (!validation.IsValid) throw new Exception($"Should have passed validation: {string.Join(", ", validation.Errors)}");
            return "Correctly accepted content at max length";
        }));

        await Task.CompletedTask;
        return results;
    }

    private async Task<List<SelfTestCaseResult>> RunComplexityTestsAsync(CancellationToken ct)
    {
        var results = new List<SelfTestCaseResult>();

        // Test 1: Unicode characters
        results.Add(await RunTestCaseAsync("Unicode_Characters", "Complexity", async () =>
        {
            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Unicode Test: 日本語 العربية 中文",
                Content = "Unicode content: café, naïve, 日本語テキスト, العربية النص, 中文内容, emoji: 🎉📄✅",
                ContentType = DocumentContentType.PlainText
            };
            var pdf = await _renderer.RenderPdfAsync(request, ct);
            if (pdf.Length == 0) throw new Exception("PDF was empty");
            return $"Generated PDF with unicode: {pdf.Length} bytes";
        }));

        // Test 2: Special characters
        results.Add(await RunTestCaseAsync("Special_Characters", "Complexity", async () =>
        {
            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Special Characters Test",
                Content = "Special chars: <>&\"' \t\n\r\n Test \"quotes\" and 'apostrophes' and <brackets>",
                ContentType = DocumentContentType.PlainText
            };
            var pdf = await _renderer.RenderPdfAsync(request, ct);
            if (pdf.Length == 0) throw new Exception("PDF was empty");
            return $"Generated PDF with special chars: {pdf.Length} bytes";
        }));

        // Test 3: Long content (near max)
        results.Add(await RunTestCaseAsync("LongContent_NearMax", "Complexity", async () =>
        {
            var longContent = string.Join("\n\n", Enumerable.Range(1, 100).Select(i => 
                $"Paragraph {i}: " + new string('x', 150)));
            
            if (longContent.Length > _options.MaxContentLength)
            {
                longContent = longContent[.._options.MaxContentLength];
            }

            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Long Content Test",
                Content = longContent,
                ContentType = DocumentContentType.PlainText
            };
            var pdf = await _renderer.RenderPdfAsync(request, ct);
            if (pdf.Length == 0) throw new Exception("PDF was empty");
            return $"Generated PDF ({longContent.Length} chars input): {pdf.Length} bytes";
        }));

        // Test 4: Complex Markdown
        results.Add(await RunTestCaseAsync("Complex_Markdown", "Complexity", async () =>
        {
            var markdown = @"# Main Heading

## Section 1

This is a paragraph with **bold**, *italic*, and `code` formatting.

### Subsection 1.1

- Bullet point 1
- Bullet point 2
  - Nested bullet
- Bullet point 3

### Subsection 1.2

1. Numbered item 1
2. Numbered item 2
3. Numbered item 3

## Section 2

> This is a blockquote with some important information.

```
Code block example
function hello() { return 'world'; }
```

## Conclusion

Final paragraph with a [link](https://example.com).";

            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Complex Markdown Test",
                Content = markdown,
                ContentType = DocumentContentType.Markdown
            };
            var pdf = await _renderer.RenderPdfAsync(request, ct);
            if (pdf.Length == 0) throw new Exception("PDF was empty");
            return $"Generated PDF from complex markdown: {pdf.Length} bytes";
        }));

        // Test 5: Complex HTML
        results.Add(await RunTestCaseAsync("Complex_Html", "Complexity", async () =>
        {
            var html = @"<html>
<body>
<h1>Main Title</h1>
<h2>Section 1</h2>
<p>This is a <strong>bold</strong> and <em>italic</em> paragraph.</p>
<ul>
<li>Item 1</li>
<li>Item 2</li>
</ul>
<h2>Section 2</h2>
<table>
<tr><th>Header 1</th><th>Header 2</th></tr>
<tr><td>Cell 1</td><td>Cell 2</td></tr>
</table>
<blockquote>A quote block</blockquote>
</body>
</html>";

            var request = new RenderDocumentRequest
            {
                TenantId = SelfTestTenantId,
                Title = "Complex HTML Test",
                Content = html,
                ContentType = DocumentContentType.Html
            };
            var pdf = await _renderer.RenderPdfAsync(request, ct);
            if (pdf.Length == 0) throw new Exception("PDF was empty");
            return $"Generated PDF from complex HTML: {pdf.Length} bytes";
        }));

        return results;
    }

    private async Task<SelfTestCaseResult> RunTestCaseAsync(string name, string category, Func<Task<string>> testAction)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            var details = await testAction();
            sw.Stop();
            
            return new SelfTestCaseResult
            {
                Name = name,
                Category = category,
                Passed = true,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Details = details
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            
            return new SelfTestCaseResult
            {
                Name = name,
                Category = category,
                Passed = false,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }

    private SelfTestCaseResult RunSyncTestCase(string name, string category, Func<string> testAction)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            var details = testAction();
            sw.Stop();
            
            return new SelfTestCaseResult
            {
                Name = name,
                Category = category,
                Passed = true,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Details = details
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            
            return new SelfTestCaseResult
            {
                Name = name,
                Category = category,
                Passed = false,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }
}

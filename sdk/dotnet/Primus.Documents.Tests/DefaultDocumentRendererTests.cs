using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Primus.Documents.Tests;

public class DefaultDocumentRendererTests
{
    private readonly IDocumentRenderer _renderer;
    private readonly DocumentRendererOptions _options;

    public DefaultDocumentRendererTests()
    {
        _options = new DocumentRendererOptions
        {
            MaxContentLength = 20000,
            SelfTestEnabled = true
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(_options));
        services.AddSingleton<IDocumentRenderer, DefaultDocumentRenderer>();
        
        var provider = services.BuildServiceProvider();
        _renderer = provider.GetRequiredService<IDocumentRenderer>();
    }

    [Fact]
    public async Task RenderPdfAsync_PlainText_ReturnsValidPdf()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "test-tenant",
            Title = "Test Document",
            Content = "This is plain text content.",
            ContentType = DocumentContentType.PlainText
        };

        // Act
        var pdfBytes = await _renderer.RenderPdfAsync(request);

        // Assert
        pdfBytes.Should().NotBeEmpty();
        pdfBytes.Length.Should().BeGreaterThan(0);
        // PDF magic bytes: %PDF
        pdfBytes[0].Should().Be(0x25); // %
        pdfBytes[1].Should().Be(0x50); // P
        pdfBytes[2].Should().Be(0x44); // D
        pdfBytes[3].Should().Be(0x46); // F
    }

    [Fact]
    public async Task RenderPdfAsync_Markdown_ReturnsValidPdf()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "test-tenant",
            Title = "Markdown Document",
            Content = "# Heading\n\nThis is **bold** and *italic* text.",
            ContentType = DocumentContentType.Markdown
        };

        // Act
        var pdfBytes = await _renderer.RenderPdfAsync(request);

        // Assert
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderPdfAsync_Html_ReturnsValidPdf()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "test-tenant",
            Title = "HTML Document",
            Content = "<h1>Hello</h1><p>This is <strong>HTML</strong> content.</p>",
            ContentType = DocumentContentType.Html
        };

        // Act
        var pdfBytes = await _renderer.RenderPdfAsync(request);

        // Assert
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderPdfAsync_WithSubtitle_ReturnsValidPdf()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "test-tenant",
            Title = "Main Title",
            Subtitle = "This is a subtitle",
            Content = "Document content here.",
            ContentType = DocumentContentType.PlainText
        };

        // Act
        var pdfBytes = await _renderer.RenderPdfAsync(request);

        // Assert
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderPdfWithResultAsync_ReturnsDetailedResult()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "test-tenant",
            Title = "Test Document",
            Content = "This is test content.",
            ContentType = DocumentContentType.PlainText
        };

        // Act
        var result = await _renderer.RenderPdfWithResultAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.PdfBytes.Should().NotBeEmpty();
        result.TenantId.Should().Be("test-tenant");
        result.Title.Should().Be("Test Document");
        result.ContentLength.Should().Be(21); // "This is test content."
        result.PdfSizeBytes.Should().BeGreaterThan(0);
        result.DurationMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void ValidateRequest_ValidRequest_ReturnsValid()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "test-tenant",
            Title = "Test",
            Content = "Content"
        };

        // Act
        var result = _renderer.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRequest_MissingTenantId_ReturnsInvalid()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "",
            Title = "Test",
            Content = "Content"
        };

        // Act
        var result = _renderer.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TenantId"));
    }

    [Fact]
    public void ValidateRequest_MissingTitle_ReturnsInvalid()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "tenant",
            Title = "",
            Content = "Content"
        };

        // Act
        var result = _renderer.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Title"));
    }

    [Fact]
    public void ValidateRequest_MissingContent_ReturnsInvalid()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "tenant",
            Title = "Test",
            Content = ""
        };

        // Act
        var result = _renderer.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Content"));
    }

    [Fact]
    public void ValidateRequest_ContentTooLong_ReturnsInvalid()
    {
        // Arrange
        var longContent = new string('x', _options.MaxContentLength + 1);
        var request = new RenderDocumentRequest
        {
            TenantId = "tenant",
            Title = "Test",
            Content = longContent
        };

        // Act
        var result = _renderer.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("maximum length"));
    }

    [Fact]
    public void ValidateRequest_ContentAtMaxLength_ReturnsValid()
    {
        // Arrange
        var maxContent = new string('x', _options.MaxContentLength);
        var request = new RenderDocumentRequest
        {
            TenantId = "tenant",
            Title = "Test",
            Content = maxContent
        };

        // Act
        var result = _renderer.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RenderPdfAsync_UnicodeContent_ReturnsValidPdf()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "test-tenant",
            Title = "Unicode Test: 日本語 العربية",
            Content = "Unicode: café, naïve, 日本語, العربية, emoji: 🎉📄",
            ContentType = DocumentContentType.PlainText
        };

        // Act
        var pdfBytes = await _renderer.RenderPdfAsync(request);

        // Assert
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderPdfAsync_SpecialCharacters_ReturnsValidPdf()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "test-tenant",
            Title = "Special Characters",
            Content = "Special: <>&\"' \t\n\r\n \"quotes\" and 'apostrophes'",
            ContentType = DocumentContentType.PlainText
        };

        // Act
        var pdfBytes = await _renderer.RenderPdfAsync(request);

        // Assert
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderPdfWithResultAsync_ValidationFails_ReturnsFailureResult()
    {
        // Arrange
        var request = new RenderDocumentRequest
        {
            TenantId = "",
            Title = "",
            Content = ""
        };

        // Act
        var result = await _renderer.RenderPdfWithResultAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.PdfBytes.Should().BeNull();
        result.Error.Should().NotBeNullOrEmpty();
    }
}

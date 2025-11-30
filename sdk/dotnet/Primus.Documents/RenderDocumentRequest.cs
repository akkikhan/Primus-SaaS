using System.ComponentModel.DataAnnotations;

namespace Primus.Documents;

/// <summary>
/// Request model for rendering a document to PDF.
/// </summary>
public sealed class RenderDocumentRequest
{
    /// <summary>
    /// The tenant ID for multi-tenant isolation.
    /// </summary>
    [Required]
    public string TenantId { get; set; } = default!;

    /// <summary>
    /// The document title. Required.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string Title { get; set; } = default!;

    /// <summary>
    /// Optional document subtitle.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// The content type of the document body.
    /// </summary>
    public DocumentContentType ContentType { get; set; } = DocumentContentType.PlainText;

    /// <summary>
    /// The document content/body to render.
    /// </summary>
    [Required]
    public string Content { get; set; } = default!;

    /// <summary>
    /// Optional metadata key-value pairs to include in logging.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; set; }
}

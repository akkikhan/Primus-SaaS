namespace Primus.Documents;

/// <summary>
/// Specifies the content type of the document to be rendered.
/// </summary>
public enum DocumentContentType
{
    /// <summary>
    /// Plain text content with no formatting.
    /// </summary>
    PlainText = 0,

    /// <summary>
    /// Markdown-formatted content.
    /// </summary>
    Markdown = 1,

    /// <summary>
    /// HTML-formatted content (trusted/sanitized).
    /// </summary>
    Html = 2
}

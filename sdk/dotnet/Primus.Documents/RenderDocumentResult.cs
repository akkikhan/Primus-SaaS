namespace Primus.Documents;

/// <summary>
/// Result of a document rendering operation.
/// </summary>
public sealed class RenderDocumentResult
{
    /// <summary>
    /// Whether the rendering was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The rendered PDF bytes. Null if rendering failed.
    /// </summary>
    public byte[]? PdfBytes { get; set; }

    /// <summary>
    /// Error message if rendering failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The tenant ID from the request.
    /// </summary>
    public string TenantId { get; set; } = default!;

    /// <summary>
    /// The document title from the request.
    /// </summary>
    public string Title { get; set; } = default!;

    /// <summary>
    /// Content length in characters.
    /// </summary>
    public int ContentLength { get; set; }

    /// <summary>
    /// PDF size in bytes (0 if failed).
    /// </summary>
    public int PdfSizeBytes { get; set; }

    /// <summary>
    /// Duration of the rendering operation in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static RenderDocumentResult Ok(byte[] pdfBytes, RenderDocumentRequest request, int durationMs) => new()
    {
        Success = true,
        PdfBytes = pdfBytes,
        TenantId = request.TenantId,
        Title = request.Title,
        ContentLength = request.Content?.Length ?? 0,
        PdfSizeBytes = pdfBytes.Length,
        DurationMs = durationMs
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static RenderDocumentResult Fail(string error, RenderDocumentRequest request, int durationMs) => new()
    {
        Success = false,
        Error = error,
        TenantId = request.TenantId,
        Title = request.Title,
        ContentLength = request.Content?.Length ?? 0,
        PdfSizeBytes = 0,
        DurationMs = durationMs
    };
}

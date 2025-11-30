namespace Primus.Documents;

/// <summary>
/// Service interface for rendering documents to PDF.
/// </summary>
public interface IDocumentRenderer
{
    /// <summary>
    /// Renders a document to PDF bytes.
    /// </summary>
    /// <param name="request">The render request containing content and options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The rendered PDF as a byte array.</returns>
    /// <exception cref="DocumentRenderException">Thrown when rendering fails.</exception>
    /// <exception cref="DocumentValidationException">Thrown when request validation fails.</exception>
    Task<byte[]> RenderPdfAsync(RenderDocumentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Renders a document to PDF with detailed result information.
    /// </summary>
    /// <param name="request">The render request containing content and options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result object containing the PDF bytes and metadata.</returns>
    Task<RenderDocumentResult> RenderPdfWithResultAsync(RenderDocumentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Validates a render request without performing the actual rendering.
    /// </summary>
    /// <param name="request">The render request to validate.</param>
    /// <returns>A validation result with any errors.</returns>
    ValidationResult ValidateRequest(RenderDocumentRequest request);
}

/// <summary>
/// Validation result for document render requests.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Whether the request is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors if invalid.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a valid result.
    /// </summary>
    public static ValidationResult Valid() => new() { IsValid = true };

    /// <summary>
    /// Creates an invalid result with errors.
    /// </summary>
    public static ValidationResult Invalid(params string[] errors) => new() 
    { 
        IsValid = false, 
        Errors = errors.ToList() 
    };
}

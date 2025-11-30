namespace Primus.Documents;

/// <summary>
/// Exception thrown when document rendering fails.
/// </summary>
public class DocumentRenderException : Exception
{
    /// <summary>
    /// The tenant ID associated with the failed operation.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// The content type that was being rendered.
    /// </summary>
    public DocumentContentType? ContentType { get; }

    public DocumentRenderException(string message) : base(message)
    {
    }

    public DocumentRenderException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public DocumentRenderException(string message, string tenantId, DocumentContentType contentType, Exception? innerException = null)
        : base(message, innerException)
    {
        TenantId = tenantId;
        ContentType = contentType;
    }
}

/// <summary>
/// Exception thrown when document request validation fails.
/// </summary>
public class DocumentValidationException : Exception
{
    /// <summary>
    /// List of validation errors.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    public DocumentValidationException(string message) : base(message)
    {
        ValidationErrors = new[] { message };
    }

    public DocumentValidationException(IEnumerable<string> errors) 
        : base($"Document validation failed: {string.Join(", ", errors)}")
    {
        ValidationErrors = errors.ToList();
    }
}

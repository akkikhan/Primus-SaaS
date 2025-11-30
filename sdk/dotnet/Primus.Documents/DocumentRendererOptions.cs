namespace Primus.Documents;

/// <summary>
/// Configuration options for the document renderer module.
/// </summary>
public class DocumentRendererOptions
{
    /// <summary>
    /// The rendering provider to use. Default is "Default" (QuestPDF-based).
    /// </summary>
    public string Provider { get; set; } = "Default";

    /// <summary>
    /// Optional path for temporary file storage when using link-based downloads.
    /// If null, uses in-memory storage.
    /// </summary>
    public string? TempStoragePath { get; set; }

    /// <summary>
    /// Maximum allowed content length in characters. Default is 20,000.
    /// </summary>
    public int MaxContentLength { get; set; } = 20000;

    /// <summary>
    /// Time-to-live for generated download links. Default is 10 minutes.
    /// </summary>
    public TimeSpan LinkTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Whether self-test endpoints are enabled. Set to false in production.
    /// </summary>
    public bool SelfTestEnabled { get; set; } = true;

    /// <summary>
    /// Default page title when none is provided.
    /// </summary>
    public string DefaultTitle { get; set; } = "Document";

    /// <summary>
    /// Company/brand name to display in the footer.
    /// </summary>
    public string? BrandName { get; set; }

    /// <summary>
    /// Whether to include a timestamp in the footer. Default is true.
    /// </summary>
    public bool IncludeTimestampInFooter { get; set; } = true;

    /// <summary>
    /// Whether to include the tenant ID in the footer. Default is true for multi-tenant scenarios.
    /// </summary>
    public bool IncludeTenantInFooter { get; set; } = true;

    /// <summary>
    /// Page margin in points. Default is 50.
    /// </summary>
    public float PageMargin { get; set; } = 50f;

    /// <summary>
    /// Font size for body text in points. Default is 12.
    /// </summary>
    public float BodyFontSize { get; set; } = 12f;

    /// <summary>
    /// Font size for title text in points. Default is 24.
    /// </summary>
    public float TitleFontSize { get; set; } = 24f;
}

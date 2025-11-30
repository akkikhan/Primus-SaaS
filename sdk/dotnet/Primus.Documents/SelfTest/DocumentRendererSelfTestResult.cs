namespace Primus.Documents.SelfTest;

/// <summary>
/// Result of a self-test run.
/// </summary>
public sealed class DocumentRendererSelfTestResult
{
    /// <summary>
    /// Whether all tests passed.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The test mode that was executed.
    /// </summary>
    public SelfTestMode Mode { get; set; }

    /// <summary>
    /// Total number of tests executed.
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// Number of tests that passed.
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    /// Number of tests that failed.
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    /// Total duration of the test run in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Individual test case results.
    /// </summary>
    public List<SelfTestCaseResult> TestCases { get; set; } = new();

    /// <summary>
    /// Summary message.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Result of an individual test case.
/// </summary>
public sealed class SelfTestCaseResult
{
    /// <summary>
    /// Name of the test case.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category of the test (Basic, Validation, Complexity).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Whether the test passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Duration of this test in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Error message if the test failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional details about the test.
    /// </summary>
    public string? Details { get; set; }
}

namespace Primus.Documents.SelfTest;

/// <summary>
/// Interface for document renderer self-test capabilities.
/// </summary>
public interface IDocumentRendererSelfTest
{
    /// <summary>
    /// Runs the self-test suite with the specified mode.
    /// </summary>
    /// <param name="mode">The test mode to run (basic, validation, complexity, full).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The self-test results.</returns>
    Task<DocumentRendererSelfTestResult> RunAsync(SelfTestMode mode, CancellationToken ct = default);
}

/// <summary>
/// Self-test execution modes.
/// </summary>
public enum SelfTestMode
{
    /// <summary>
    /// Basic smoke test: minimal valid input renders successfully.
    /// </summary>
    Basic,

    /// <summary>
    /// Validation tests: invalid inputs are rejected appropriately.
    /// </summary>
    Validation,

    /// <summary>
    /// Complexity tests: long content, unicode, special characters.
    /// </summary>
    Complexity,

    /// <summary>
    /// Full test suite: runs all test modes.
    /// </summary>
    Full
}

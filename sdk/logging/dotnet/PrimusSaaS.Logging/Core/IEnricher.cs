namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Interface for custom log enrichers
/// </summary>
public interface IEnricher
{
    /// <summary>
    /// Enrich the log context with additional properties
    /// </summary>
    /// <param name="context">The log context dictionary to modify</param>
    void Enrich(Dictionary<string, object?> context);
}

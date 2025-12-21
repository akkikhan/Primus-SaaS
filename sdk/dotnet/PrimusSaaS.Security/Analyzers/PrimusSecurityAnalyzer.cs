using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PrimusSaaS.Security.Analyzers;

/// <summary>
/// Base class for all Primus Security analyzers.
/// </summary>
public abstract class PrimusSecurityAnalyzer : DiagnosticAnalyzer
{
    // Common logic for all our analyzers
    protected static DiagnosticDescriptor CreateDescriptor(
        string id,
        string title,
        string messageFormat,
        string description,
        string category = "Security",
        DiagnosticSeverity severity = DiagnosticSeverity.Warning)
    {
        return new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            category,
            severity,
            isEnabledByDefault: true,
            description: description,
            helpLinkUri: $"https://docs.primussaas.com/security/rules/{id}");
    }
}

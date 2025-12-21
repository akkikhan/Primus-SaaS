using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PrimusSaaS.Security.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HardcodedSecretAnalyzer : PrimusSecurityAnalyzer
{
    public const string DiagnosticId = "PS0003";

    private static readonly DiagnosticDescriptor Rule = CreateDescriptor(
        DiagnosticId,
        "Hardcoded Secret Detected",
        "Potential hardcoded secret found in code. Move this value to configuration or environment variables.",
        "Detects string literals that resemble API keys, tokens, or passwords.",
        severity: DiagnosticSeverity.Error);

    // Common prefixes that are almost always secrets
    private static readonly Regex[] HighConfidencePatterns = new[]
    {
        new Regex(@"^AKIA[0-9A-Z]{16}$", RegexOptions.Compiled), // AWS Access Key
        new Regex(@"^sk_live_[0-9a-zA-Z]{24}", RegexOptions.Compiled), // Stripe
        new Regex(@"^xox[baprs]-([0-9a-zA-Z]{10,48})", RegexOptions.Compiled), // Slack
        new Regex(@"^gh[pousr]_[A-Za-z0-9_]{36,255}", RegexOptions.Compiled), // GitHub
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
    }

    private void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var literal = (LiteralExpressionSyntax)context.Node;
        var token = literal.Token;
        var text = token.ValueText;

        if (string.IsNullOrEmpty(text) || text.Length < 10) return;

        // Check against known patterns
        foreach (var pattern in HighConfidencePatterns)
        {
            if (pattern.IsMatch(text))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation()));
                return; // Found a match, no need to check further
            }
        }

        // Heuristic: Check if variable name suggests a secret AND the value looks suspicious
        if (IsAssignedToSecretVariable(literal))
        {
            // If assigned to "password" or "secret" and length > 8 and high entropy (mocked by complexity)
            if (text.Length > 8 && HasComplexity(text))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation()));
            }
        }
    }

    private bool IsAssignedToSecretVariable(LiteralExpressionSyntax literal)
    {
        // var secret = "..."
        if (literal.Parent is EqualsValueClauseSyntax equals && 
            equals.Parent is VariableDeclaratorSyntax declarator)
        {
            var name = declarator.Identifier.Text.ToLowerInvariant();
            return IsSuspectName(name);
        }

        // secret = "..."
        if (literal.Parent is AssignmentExpressionSyntax assignment && 
            assignment.Left is IdentifierNameSyntax identifier)
        {
             var name = identifier.Identifier.Text.ToLowerInvariant();
             return IsSuspectName(name);
        }

        return false;
    }

    private bool IsSuspectName(string name)
    {
        return name.Contains("password") || 
               name.Contains("secret") || 
               name.Contains("apikey") || 
               name.Contains("token") ||
               name.Contains("auth");
    }

    private bool HasComplexity(string text)
    {
        // Simple heuristic: Mixed case + numbers, or simply nice and long random-looking
        // Should catch "CorrectHorseBatteryStaple" (maybe false positive?)
        // Better: catch things that don't look like sentences.
        if (text.Contains(" ")) return false; // Sentences probably aren't secrets (unless passphrases)
        
        bool hasUpper = text.Any(char.IsUpper);
        bool hasLower = text.Any(char.IsLower);
        bool hasDigit = text.Any(char.IsDigit);

        return hasUpper && hasLower && hasDigit;
    }
}

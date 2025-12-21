using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PrimusSaaS.Security.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class XssAnalyzer : PrimusSecurityAnalyzer
{
    public const string DiagnosticId = "PS0002";

    private static readonly DiagnosticDescriptor Rule = CreateDescriptor(
        DiagnosticId,
        "Potential Cross-Site Scripting (XSS)",
        "Use of '{0}' with untrusted input can lead to XSS. Ensure content is sanitized or use an encoder.",
        "Detects usage of raw HTML rendering methods which bypass auto-encoding.",
        severity: DiagnosticSeverity.Warning);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

        if (methodSymbol == null) return;

        // Check for Html.Raw()
        if (IsDangerousMethod(methodSymbol))
        {
            // If the argument is a constant string, it's usually safe (e.g. Html.Raw("<b>Bold</b>"))
            // We only care about dynamic input.
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var arg = invocation.ArgumentList.Arguments[0];
                var constantValue = context.SemanticModel.GetConstantValue(arg.Expression);

                if (!constantValue.HasValue)
                {
                    // It's not a constant, potentially unsafe
                    context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodSymbol.Name));
                }
            }
        }
    }

    private bool IsDangerousMethod(IMethodSymbol method)
    {
        // HtmlHelper.Raw
        if (method.Name == "Raw" && method.ContainingType.Name.Contains("HtmlHelper")) return true;
        
        // IHtmlHelper.Raw
        if (method.Name == "Raw" && method.ContainingType.Name.Contains("IHtmlHelper")) return true;

        // Microsoft.AspNetCore.Html.HtmlString constructor (often used to bypass encoding)
        if (method.MethodKind == MethodKind.Constructor && method.ContainingType.Name == "HtmlString") return true;

        return false;
    }
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PrimusSaaS.Security.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SqlInjectionAnalyzer : PrimusSecurityAnalyzer
{
    public const string DiagnosticId = "PS0001";

    private static readonly DiagnosticDescriptor Rule = CreateDescriptor(
        DiagnosticId,
        "Possible SQL Injection vulnerability",
        "Possible SQL Injection detected. Avoid concatenating strings into SQL queries. Use parameterized queries instead.",
        "SQL Injection occurs when untrusted data is concatenated directly into a database query string.",
        severity: DiagnosticSeverity.Error);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

        if (methodSymbol == null) return;

        // Check for common SQL execution methods
        // e.g. SqlCommand.ExecuteReader(cmdText), FromSqlRaw(sql), ExecuteSqlRaw(sql)
        if (IsSqlExecutionMethod(methodSymbol))
        {
            // Check the first argument (usually the SQL string)
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var sqlArg = invocation.ArgumentList.Arguments[0];
                var sqlExpr = sqlArg.Expression;

                // Check if the argument is a string concatenation or interpolation
                if (IsConcatenationOrInterpolation(sqlExpr, context.SemanticModel))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, sqlExpr.GetLocation()));
                }
            }
        }
    }

    private bool IsSqlExecutionMethod(IMethodSymbol method)
    {
        // Add more methods here (Dapper, EF Core, ADO.NET)
        var typeName = method.ContainingType.Name;
        var methodName = method.Name;

        if (typeName == "SqlCommand" && (methodName.StartsWith("Execute") || methodName == "Constructor")) return true;
        if (methodName == "FromSqlRaw" || methodName == "ExecuteSqlRaw") return true; // EF Core
        if (methodName == "Query" || methodName == "QueryAsync") return true; // Dapper

        return false;
    }

    private bool IsConcatenationOrInterpolation(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        // 1. String Interpolation: $"SELECT * FROM Users WHERE Id = {id}"
        if (expression.IsKind(SyntaxKind.InterpolatedStringExpression))
        {
            return true;
        }

        // 2. String Concatenation: "SELECT * FROM Users WHERE Id = " + id
        if (expression.IsKind(SyntaxKind.AddExpression))
        {
            // Simple heuristic: if type information is string
            var typeInfo = semanticModel.GetTypeInfo(expression);
            if (typeInfo.Type?.SpecialType == SpecialType.System_String)
            {
                return true;
            }
        }

        // 3. String.Format: String.Format("SELECT ... {0}", id)
        if (expression is InvocationExpressionSyntax invocation)
        {
             var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
             if (methodSymbol?.ContainingType.SpecialType == SpecialType.System_String && methodSymbol.Name == "Format")
             {
                 return true;
             }
        }

        return false;
    }
}

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Simple, defensive call-site converter:
// - Default: dry-run (reports planned edits).
// - Converts ILogger LogInformation/LogWarning/LogError/LogCritical/LogDebug/LogTrace to Primus Logger calls.
// - Only rewrites when the message is a string literal (optionally with structured placeholders).
// - Skips risky constructs (non-literal messages, unknown overloads) and reports them.

var argsDict = ParseArgs(args);
var rootPath = argsDict.GetValueOrDefault("path") ?? Directory.GetCurrentDirectory();
var write = argsDict.ContainsKey("write");

var files = Directory.EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories)
    .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
    .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
    .ToList();

var summary = new List<string>();
var converted = 0;
var skipped = 0;

foreach (var file in files)
{
    var text = await File.ReadAllTextAsync(file);
    var tree = CSharpSyntaxTree.ParseText(text);
    var rewriter = new LoggerRewriter();
    var newRoot = rewriter.Visit(tree.GetRoot());

    if (rewriter.Edits.Count == 0 && rewriter.Skips.Count == 0)
    {
        continue;
    }

    foreach (var edit in rewriter.Edits)
    {
        summary.Add($"{file}: converted {edit}");
        converted++;
    }

    foreach (var skip in rewriter.Skips)
    {
        summary.Add($"{file}: skipped {skip}");
        skipped++;
    }

    if (write && rewriter.Edits.Count > 0)
    {
        var newText = newRoot.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(file, newText, Encoding.UTF8);
    }
}

Console.WriteLine($"Primus call-site converter ({(write ? "WRITE" : "DRY-RUN")})");
Console.WriteLine($"Scanned {files.Count} files. Conversions: {converted}, Skips: {skipped}");
foreach (var line in summary)
{
    Console.WriteLine(line);
}

// --- helpers ---

static Dictionary<string, string> ParseArgs(IEnumerable<string> input)
{
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var arg in input)
    {
        if (arg.StartsWith("--path=", StringComparison.OrdinalIgnoreCase))
        {
            dict["path"] = arg.Substring("--path=".Length);
        }
        else if (arg.Equals("--write", StringComparison.OrdinalIgnoreCase))
        {
            dict["write"] = "true";
        }
    }
    return dict;
}

internal sealed class LoggerRewriter : CSharpSyntaxRewriter
{
    private static readonly ImmutableDictionary<string, string> MethodMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["LogInformation"] = "Info",
        ["LogWarning"] = "Warn",
        ["LogError"] = "Error",
        ["LogCritical"] = "Critical",
        ["LogDebug"] = "Debug",
        ["LogTrace"] = "Debug"
    }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    public List<string> Edits { get; } = new();
    public List<string> Skips { get; } = new();

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var expression = node.Expression;
        if (expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return base.VisitInvocationExpression(node);
        }

        var methodName = memberAccess.Name.Identifier.Text;
        if (!MethodMap.TryGetValue(methodName, out var primusMethod))
        {
            return base.VisitInvocationExpression(node);
        }

        var args = node.ArgumentList.Arguments;
        if (args.Count == 0)
        {
            Skips.Add("no-args");
            return base.VisitInvocationExpression(node);
        }

        // Identify message and exception positions.
        LiteralExpressionSyntax? messageLiteral = null;
        ExpressionSyntax? exceptionExpr = null;
        List<ArgumentSyntax> templateArgs;

        if (args[0].Expression is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
        {
            messageLiteral = lit;
            templateArgs = args.Count > 1 ? args.Skip(1).ToList() : new List<ArgumentSyntax>();
        }
        else if (args.Count >= 2 && args[1].Expression is LiteralExpressionSyntax lit2 && lit2.IsKind(SyntaxKind.StringLiteralExpression))
        {
            exceptionExpr = args[0].Expression;
            messageLiteral = lit2;
            templateArgs = args.Count > 2 ? args.Skip(2).ToList() : new List<ArgumentSyntax>();
        }
        else
        {
            Skips.Add("non-literal-message");
            return base.VisitInvocationExpression(node);
        }

        var messageText = messageLiteral.Token.ValueText;
        var placeholders = ExtractPlaceholders(messageText);
        var contextInit = BuildContextInitializer(placeholders, templateArgs);

        var argumentList = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(messageLiteral.WithTriviaFrom(messageLiteral))
        };

        if (contextInit != null)
        {
            argumentList.Add(SyntaxFactory.Argument(contextInit));
        }

        ExpressionSyntax rewritten;

        if (exceptionExpr != null)
        {
            rewritten = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    memberAccess.Expression,
                    SyntaxFactory.IdentifierName(primusMethod)),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                    new[]
                    {
                        SyntaxFactory.Argument(exceptionExpr),
                        argumentList[0],
                        argumentList.Count > 1 ? argumentList[1] : SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
                    })));
        }
        else
        {
            rewritten = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    memberAccess.Expression,
                    SyntaxFactory.IdentifierName(primusMethod)),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentList)));
        }

        Edits.Add($"{methodName}→{primusMethod}");
        return rewritten;
    }

    private static ObjectCreationExpressionSyntax? BuildContextInitializer(List<string> placeholders, List<ArgumentSyntax> args)
    {
        if (placeholders.Count == 0 || args.Count == 0)
        {
            return null;
        }

        var assignments = new List<ExpressionSyntax>();
        var count = Math.Min(placeholders.Count, args.Count);
        for (var i = 0; i < count; i++)
        {
            var key = placeholders[i];
            var valueExpr = args[i].Expression;
            assignments.Add(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.ImplicitElementAccess(SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(key)))))),
                    valueExpr));
        }

        // Remaining args without placeholders get argN keys
        for (var i = count; i < args.Count; i++)
        {
            var key = $"arg{i - count}";
            assignments.Add(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.ImplicitElementAccess(SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(key)))))),
                    args[i].Expression));
        }

        return SyntaxFactory.ObjectCreationExpression(
            SyntaxFactory.ParseTypeName("System.Collections.Generic.Dictionary<string, object?>"))
            .WithInitializer(
                SyntaxFactory.InitializerExpression(SyntaxKind.CollectionInitializerExpression,
                    SyntaxFactory.SeparatedList(assignments)));
    }

    private static List<string> ExtractPlaceholders(string message)
    {
        var results = new List<string>();
        var span = message.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == '{')
            {
                var end = span.Slice(i + 1).IndexOf('}');
                if (end > 0)
                {
                    var placeholder = span.Slice(i + 1, end).ToString();
                    placeholder = placeholder.Trim();
                    placeholder = placeholder.TrimStart('@').Split(':')[0]; // strip destructuring or format
                    if (!string.IsNullOrWhiteSpace(placeholder))
                    {
                        results.Add(placeholder);
                    }
                    i += end;
                }
            }
        }

        return results;
    }
}

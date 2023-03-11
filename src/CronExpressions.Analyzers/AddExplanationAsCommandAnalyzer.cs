using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Cronos;

namespace CronExpressions.Analyers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddExplanationAsCommandAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CRON003";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Add explanation as comment", "Add explanation as comment", Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: "Add a human-readable explanation of a cron expression as a comment");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(ctx =>
            {
                var stringLiteralExpr = (LiteralExpressionSyntax)ctx.Node;
                var str = stringLiteralExpr.Token.ValueText;
                if (string.IsNullOrWhiteSpace(str)) return;
                str = str.TrimStart('\"').TrimEnd('\"').ToLower();
                if (string.IsNullOrWhiteSpace(str)) return;

                var splitted = str.Split(' ');
                if (splitted.Length < 5 || splitted.Length > 6) return;
                try
                {
                    CronExpression.Parse(str, splitted.Length == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard);
                    var diagnostic = Diagnostic.Create(Rule, stringLiteralExpr.GetLocation());
                    ctx.ReportDiagnostic(diagnostic);
                }
                catch
                {
                    return;
                }
            }, SyntaxKind.StringLiteralExpression);
        }
    }
}

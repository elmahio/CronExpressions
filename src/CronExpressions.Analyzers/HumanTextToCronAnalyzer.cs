using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace CronExpressions.Analyers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HumanTextToCronAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CRON001";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "To Cron expression", "To Cron expression", Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: "Convert a human-readable string to a Cron expression");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(ctx =>
            {
                var stringLiteralExpr = (LiteralExpressionSyntax)ctx.Node;
                var str = stringLiteralExpr.ToFullString();
                if (string.IsNullOrWhiteSpace(str)) return;
                str = str.TrimStart('\"').TrimEnd('\"').ToLower();
                if (string.IsNullOrWhiteSpace(str)) return;

                if (str.Contains("every ")
                    || str.Contains("at ")
                    || str.Contains("once")
                    || str.Contains("each "))
                {
                    var diagnostic = Diagnostic.Create(Rule, stringLiteralExpr.GetLocation());
                    ctx.ReportDiagnostic(diagnostic);
                }
            }, SyntaxKind.StringLiteralExpression);
        }
    }
}

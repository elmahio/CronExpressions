using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;
using System;

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
                var str = stringLiteralExpr.Token.ValueText;
                if (ShouldReport(str))
                {
                    var diagnostic = Diagnostic.Create(Rule, stringLiteralExpr.GetLocation());
                    ctx.ReportDiagnostic(diagnostic);
                }

            }, SyntaxKind.StringLiteralExpression);
        }

        public static bool ShouldReport(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return false;
            str = str.TrimStart('\"').TrimEnd('\"').ToLower();
            if (string.IsNullOrWhiteSpace(str)) return false;

            var terms = str.ToLowerInvariant().Split(' ');
            if (terms.Contains("once")) return true;
            else
            {
                for (var i = 0; i < terms.Length; i++)
                {
                    var term = terms[i];
                    if (term == "every" && terms.Length > 1+i) return true;
                    else if (term == "at" && terms.Length > 1+i) return true;
                    else if (term == "each" && terms.Length > 1+i) return true;
                }
            }

            return false;
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CronExpressions.Analyers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IncorrectCronAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CRON002";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Invalid Cron expression", "Invalid Cron expression", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "Find incorrect Cron expressions in C# code");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(ctx =>
            {
                var stringLiteralExpr = (LiteralExpressionSyntax)ctx.Node;
                var parent = stringLiteralExpr.Parent.Parent.Parent;

                var str = stringLiteralExpr.Token.ValueText;
                if (string.IsNullOrWhiteSpace(str)) return;
                str = str.TrimStart('\"').TrimEnd('\"').ToLower();
                if (string.IsNullOrWhiteSpace(str)) return;

                // CronExpression.Parse("* * * * * *")
                if (parent is InvocationExpressionSyntax ies)
                {
                    var maes = ies.Expression as MemberAccessExpressionSyntax;
                    if (maes == null) return;
                    var typeName = maes.Expression as IdentifierNameSyntax;
                    var methodName = maes.Name as IdentifierNameSyntax;
                    if (typeName == null || methodName == null) return;
                    var type = typeName.Identifier.ValueText;
                    var method = methodName.Identifier.ValueText;
                    if (type == "CronExpression" && method == "Parse")
                    {
                        ReportIfInvalid(ctx, stringLiteralExpr, str, false);
                    }
                }
                // [TimerTrigger("* * * * *")]
                else if (parent is AttributeSyntax @as)
                {
                    var name = @as.Name as IdentifierNameSyntax;
                    if (name == null) return;
                    var type = name.Identifier.ValueText;
                    if (type == "TimerTrigger")
                    {
                        ReportIfInvalid(ctx, stringLiteralExpr, str, true);
                    }
                }
                // new CronTimer("* * * * *")
                // new CronJob("* * * * *")
                // new CronSchedule("* * * * *")
                else if (parent is ObjectCreationExpressionSyntax oces)
                {
                    var ins = oces.Type as IdentifierNameSyntax;
                    if (ins == null) return;
                    var type = ins.Identifier.ValueText;
                    if (type == "CronTimer" || type == "CronJob" || type == "CronSchedule")
                    {
                        ReportIfInvalid(ctx, stringLiteralExpr, str, false);
                    }
                }
            }, SyntaxKind.StringLiteralExpression);
        }

        private static void ReportIfInvalid(SyntaxNodeAnalysisContext ctx, LiteralExpressionSyntax stringLiteralExpr, string str, bool includeSeconds)
        {
            try
            {
                Cronos.CronExpression.Parse(str, includeSeconds ? Cronos.CronFormat.IncludeSeconds : Cronos.CronFormat.Standard);
            }
            catch
            {
                var diagnostic = Diagnostic.Create(Rule, stringLiteralExpr.GetLocation());
                ctx.ReportDiagnostic(diagnostic);
            }
        }
    }
}

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
                // CrontabSchedule.Parse("* * * * * *")
                // CrontabSchedule.TryParse("* * * * * *")
                if (parent is InvocationExpressionSyntax ies)
                {
                    if (!(ies.Expression is MemberAccessExpressionSyntax maes)) return;
                    if (!(maes.Expression is IdentifierNameSyntax typeName) || !(maes.Name is IdentifierNameSyntax methodName)) return;
                    var type = typeName.Identifier.ValueText;
                    var method = methodName.Identifier.ValueText;
                    if (type == "CronExpression" && method == "Parse")
                    {
                        ReportIfInvalid(ctx, stringLiteralExpr, str, false);
                    }
                    else if (type == "CrontabSchedule" && (method == "Parse" || method == "TryParse"))
                    {
                        ReportIfInvalid(ctx, stringLiteralExpr, str, false);
                    }
                }
                // [TimerTrigger("* * * * *")]
                else if (parent is AttributeSyntax @as)
                {
                    if (!(@as.Name is IdentifierNameSyntax name)) return;
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
                    if (!(oces.Type is IdentifierNameSyntax ins)) return;
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

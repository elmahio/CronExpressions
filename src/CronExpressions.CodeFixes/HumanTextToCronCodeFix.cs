using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis;
using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using CronExpressions.Analyers;
using norC;

namespace Analyzer1
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(HumanTextToCronCodeFix)), Shared]
    public class HumanTextToCronCodeFix : CodeFixProvider
    {
        private const string title = "Replace string with another string";
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(HumanTextToCronAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        private async Task<Solution> ReplaceStringAsync(Document document, LiteralExpressionSyntax literal, bool includeSeconds, CancellationToken cancellationToken)
        {
            var str = literal.ToFullString().TrimStart('\"').TrimEnd('\"');
            var cron = str.AsCronString(new CronOptions { IncludeSeconds = includeSeconds });
            var newLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(cron));

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            root = root.ReplaceNode(literal, newLiteral);

            return document.WithSyntaxRoot(root).Project.Solution;
        }
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var literal = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedSolution: c => ReplaceStringAsync(context.Document, literal, false, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixWithSecondsTitle,
                    createChangedSolution: c => ReplaceStringAsync(context.Document, literal, true, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixWithSecondsTitle)),
                diagnostic);
        }
    }
}

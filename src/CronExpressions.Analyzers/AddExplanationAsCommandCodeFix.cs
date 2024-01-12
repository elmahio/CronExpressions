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
using System.Globalization;
using CronExpressionDescriptor;

namespace CronExpressions.Analyers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddExplanationAsCommandCodeFix)), Shared]
    public class AddExplanationAsCommandCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AddExplanationAsCommandAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        private async Task<Solution> AppendComment(Document document, LiteralExpressionSyntax literal, CancellationToken cancellationToken)
        {
            var str = literal.Token.ValueText.TrimStart('\"').TrimEnd('\"');
            var message = ExpressionDescriptor.GetDescription(str, new Options
            {
                Use24HourTimeFormat = DateTimeFormatInfo.CurrentInfo.ShortTimePattern.Contains("H"),
                ThrowExceptionOnParseError = false,
                Verbose = true,
            });
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(message))
            {
                var newLiteral = literal.WithTrailingTrivia(SyntaxFactory.Comment($"/* {message} */"));
                root = root.ReplaceNode(literal, newLiteral);
            }

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
                    title: "Add explanation as comment",
                    createChangedSolution: c => AppendComment(context.Document, literal, c)),
                diagnostic);
        }
    }
}

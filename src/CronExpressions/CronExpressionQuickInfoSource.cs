using CronExpressionDescriptor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CronExpressions
{
    public class CronExpressionQuickInfoSource : IAsyncQuickInfoSource
    {
        private ITextBuffer textBuffer;

        public CronExpressionQuickInfoSource(ITextBuffer textBuffer)
        {
            this.textBuffer = textBuffer;
        }

        public void Dispose()
        {
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            // The code in this method is based on this amazing video:
            // https://www.youtube.com/watch?v=s0OrtzpNjtc&ab_channel=LearnRoslynbyexample

            try
            {
                var snapshot = textBuffer.CurrentSnapshot;

                var triggerPoint = session.GetTriggerPoint(snapshot);
                if (triggerPoint is null) return null;

                var position = triggerPoint.Value.Position;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                var quickInfo = await CalculateQuickInfoAsync(document, position, cancellationToken);
                if (quickInfo is null) return null;

                return new QuickInfoItem(
                    snapshot.CreateTrackingSpan(
                        new Span(quickInfo.Value.span.Start, quickInfo.Value.span.Length),
                        SpanTrackingMode.EdgeExclusive),
                    new ContainerElement(ContainerElementStyle.Stacked, quickInfo.Value.message));
            }
            catch
            {
                // No need to crash the user's Visual Studio
                return null;
            }
        }

        public static async Task<(List<object> message, TextSpan span)?> CalculateQuickInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var rootNode = await document.GetSyntaxRootAsync(cancellationToken);
            var node = rootNode.FindNode(TextSpan.FromBounds(position, position));

            if (!(node is SyntaxNode identifier)) return null;

            LiteralExpressionSyntax literalExpressionSyntax = null;

            if (node is ArgumentSyntax argumentSyntax)
                literalExpressionSyntax = argumentSyntax.Expression as LiteralExpressionSyntax;
            else if (node is AttributeArgumentSyntax attributeArgumentSyntax)
                literalExpressionSyntax = attributeArgumentSyntax.Expression as LiteralExpressionSyntax;
            else if (node is ExpressionStatementSyntax expressionStatementSyntax)
                literalExpressionSyntax = expressionStatementSyntax.Expression as LiteralExpressionSyntax;
            else if (node is LiteralExpressionSyntax literalExpressionSyntax1)
                literalExpressionSyntax = literalExpressionSyntax1;

            if (literalExpressionSyntax == null) return null;
            else if (literalExpressionSyntax.Kind() != Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression) return null;

            var text = literalExpressionSyntax.Token.ValueText?.ToString();
            if (string.IsNullOrWhiteSpace(text)) return null;

            var expression = text.TrimStart('\"').TrimEnd('\"');

            string message = null;
            try
            {
                message = ExpressionDescriptor.GetDescription(expression, new Options
                {
                    Use24HourTimeFormat = DateTimeFormatInfo.CurrentInfo.ShortTimePattern.Contains("H"),
                    ThrowExceptionOnParseError = false,
                    Verbose = true,
                });
            }
            catch
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(message) || message.StartsWith("error: ", System.StringComparison.InvariantCultureIgnoreCase)) return null;

            var stackedElements = new List<object>
                {
                    message,
                    ClassifiedTextElement.CreateHyperlink("More details", "Click here to see more details about this expression", () =>
                    {
                        Process.Start($"https://elmah.io/tools/cron-parser/#{expression.Replace(' ', '_')}");
                    })
                };

            var span = identifier.Span;
            return (stackedElements, span);
        }
    }
}
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
        private readonly ITextBuffer textBuffer;

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

                (List<object> message, TextSpan span)? quickInfo = null;

                if (document != null)
                {
                    // C# file
                    quickInfo = await CalculateQuickInfoAsync(document, position, cancellationToken);
                }
                else
                {
                    // Non-Roslyn file
                    quickInfo = CalculateQuickInfoTextFile(snapshot, position);
                }

                if (quickInfo is null || quickInfo.Value.message is null || quickInfo.Value.span == default) return null;

                return new QuickInfoItem(
                    snapshot.CreateTrackingSpan(
                        new Span(quickInfo.Value.span.Start, quickInfo.Value.span.Length),
                        SpanTrackingMode.EdgeExclusive),
                    new ContainerElement(ContainerElementStyle.Stacked, quickInfo.Value.message));
            }
            catch
            {
                // No need to crash the user's Visual Studio
            }

            return null;
        }

        private static (List<object> message, TextSpan span)? CalculateQuickInfoTextFile(ITextSnapshot snapshot, int position)
        {
            var line = snapshot.GetLineFromPosition(position);
            var lineText = line.GetText();

            var parts = lineText.Split(':');
            if (parts.Length < 2) return null;

            var cronExpression = parts[parts.Length - 1];
            return CalculateQuickInfoElements(cronExpression, line.Start + lineText.LastIndexOf(':') + 1);
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
            else if (node is LiteralExpressionSyntax literalExpressionSyntax1)
                literalExpressionSyntax = literalExpressionSyntax1;

            if (literalExpressionSyntax == null || literalExpressionSyntax.Kind() != Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression)
                return null;

            var text = literalExpressionSyntax.Token.ValueText;
            if (string.IsNullOrWhiteSpace(text)) return null;

            var expression = text.Trim('\"');
            return CalculateQuickInfoElements(expression, identifier.Span.Start);
        }

        private static (List<object> message, TextSpan span)? CalculateQuickInfoElements(string cronExpression, int spanStart)
        {
            // Trim the CRON expression and validate it's not empty
            cronExpression = cronExpression.Trim('\'', '"', ' ');
            if (string.IsNullOrWhiteSpace(cronExpression)) return null;

            // Try to get the description from the CRON expression
            string description = null;
            try
            {
                description = ExpressionDescriptor.GetDescription(cronExpression, new Options
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

            // Check if description is valid
            if (string.IsNullOrWhiteSpace(description) || description.StartsWith("error: ", System.StringComparison.InvariantCultureIgnoreCase))
                return null;

            // Prepare elements for the quick info popup
            var stackedElements = new List<object>
            {
                description,
                ClassifiedTextElement.CreateHyperlink("More details", "Click here to see more details about this expression", () =>
                {
                    Process.Start($"https://elmah.io/tools/cron-parser/#{cronExpression.Replace(' ', '_')}");
                })
            };
            var span = new TextSpan(spanStart, cronExpression.Length);

            return (stackedElements, span);
        }
    }
}
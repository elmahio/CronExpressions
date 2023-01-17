using CronExpressions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace CronExpressionsVsix
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Quick info when hovering cron expressions")]
    [Order(After = "default")]
    [ContentType("CSharp")]
    public class CronExpressions : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new CronExpressionQuickInfoSource(textBuffer);
        }
    }
}

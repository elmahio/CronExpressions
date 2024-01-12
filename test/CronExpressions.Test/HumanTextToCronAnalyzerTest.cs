#pragma warning disable CRON001 // To Cron expression
using CronExpressions.Analyers;
using NUnit.Framework;

namespace CronExpressions.Test
{
    public class HumanTextToCronAnalyzerTest
    {
        [TestCase("once", true)]
        [TestCase("every hour", true)]
        [TestCase("every", false)]
        [TestCase("hour every", false)]
        [TestCase("at 12 pm", true)]
        [TestCase("at 12", true)]
        [TestCase("at", false)]
        [TestCase("about at", false)]
        [TestCase("each hour", true)]
        [TestCase("each", false)]
        [TestCase("about each", false)]
        [TestCase("", false)]
        [TestCase("once\r\nevery hour", false)]
        [TestCase("once\nevery hour", false)]
        [TestCase("reach ", false)] // https://github.com/elmahio/CronExpressions/issues/4
        public void CanShouldReport(string input, bool expected) => Assert.That(HumanTextToCronAnalyzer.ShouldReport(input), Is.EqualTo(expected));
    }
}
#pragma warning restore CRON001 // To Cron expression

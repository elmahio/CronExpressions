#pragma warning disable CRON001 // To Cron expression
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CronExpressions.Test
{
    public class CalculateQuickInfoTests
    {
        private const string Five = "* * *¤ * *";
        private const string Six = "0 * *¤ * * *";

        [TestCase(Five)]
        [TestCase(Six)]
        public async Task CanFindExpressionInParameterAsync(string expression)
        {
            var code = @"
public class Test
{
    public void Method1(string expression)
    {
    }

    public void Method2()
    {
        Method1(""CRON"");
    }
}".Replace("CRON", expression);

            var position = code.IndexOf("¤");
            var document = Document(code);

            var result = await CronExpressionQuickInfoSource.CalculateQuickInfoAsync(document, position, CancellationToken.None);

            Assert.That(result.HasValue);
            Assert.That(result.Value.message, Is.Not.Null);
            Assert.That(result.Value.message.Count, Is.EqualTo(2));
            Assert.That(result.Value.message[0], Is.EqualTo("Every minute, every hour, every day"));
        }

        [TestCase(Five)]
        [TestCase(Six)]
        public async Task CanFindExpressionInMethodAttributeAsync(string expression)
        {
            var code = @"
public class Test
{
    [Trigger(""CRON"")]
    public void Method1(string expression)
    {
    }
}".Replace("CRON", expression);

            var position = code.IndexOf("¤");
            var document = Document(code);

            var result = await CronExpressionQuickInfoSource.CalculateQuickInfoAsync(document, position, CancellationToken.None);

            Assert.That(result.HasValue);
            Assert.That(result.Value.message, Is.Not.Null);
            Assert.That(result.Value.message.Count, Is.EqualTo(2));
            Assert.That(result.Value.message[0], Is.EqualTo("Every minute, every hour, every day"));
        }

        [TestCase(Five)]
        [TestCase(Six)]
        public async Task CanFindExpressionInClassAttributeAsync(string expression)
        {
            var code = @"
[Trigger(""CRON"")]
public class Test
{
}".Replace("CRON", expression);

            var position = code.IndexOf("¤");
            var document = Document(code);

            var result = await CronExpressionQuickInfoSource.CalculateQuickInfoAsync(document, position, CancellationToken.None);

            Assert.That(result.HasValue);
            Assert.That(result.Value.message, Is.Not.Null);
            Assert.That(result.Value.message.Count, Is.EqualTo(2));
            Assert.That(result.Value.message[0], Is.EqualTo("Every minute, every hour, every day"));
        }

        [TestCase(Five)]
        [TestCase(Six)]
        public async Task CanFindExpressionInVariableAsync(string expression)
        {
            var code = @"
public class Test
{
    private string expression = ""CRON"";
}".Replace("CRON", expression);

            var position = code.IndexOf("¤");
            var document = Document(code);

            var result = await CronExpressionQuickInfoSource.CalculateQuickInfoAsync(document, position, CancellationToken.None);

            Assert.That(result.HasValue);
            Assert.That(result.Value.message, Is.Not.Null);
            Assert.That(result.Value.message.Count, Is.EqualTo(2));
            Assert.That(result.Value.message[0], Is.EqualTo("Every minute, every hour, every day"));
        }

        [TestCase("da-DK", "At 08:01, every day, every 4 days of the week, every 2 months")]
        [TestCase("en-US", "At 08:01 AM, every day, every 4 days of the week, every 2 months")]
        public async Task CultureTestAsync(string culture, string expectedMessage)
        {
            var code = @"
public class Test
{
    private string expression = ""0 1 8 *¤ */2 */4"";
}";

            var position = code.IndexOf("¤");
            var document = Document(code);
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            var result = await CronExpressionQuickInfoSource.CalculateQuickInfoAsync(document, position, CancellationToken.None);

            Assert.That(result.Value.message[0], Is.EqualTo(expectedMessage));
        }

        private static Document Document(string code)
        {
            var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            solution = solution.AddProject(projectId, "Project1", "Project1", LanguageNames.CSharp);

            var project = solution.GetProject(projectId);
            project = project.AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            var document = project.AddDocument("File1.cs", code.Replace("¤", ""));
            return document;
        }
    }
}
#pragma warning restore CRON001 // To Cron expression

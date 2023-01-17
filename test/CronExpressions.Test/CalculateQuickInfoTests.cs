using CronExpressions.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CronExpressions.Test
{
    public class CalculateQuickInfoTests
    {
        [Test]
        public async Task CanFindExpressionInParameter()
        {
            var code = @"
public class Test
{
    public void Method1(string expression)
    {
    }

    public void Method2()
    {
        Method1(""* * *¤ * *"");
    }
}";

            var position = code.IndexOf("¤");
            var document = Document(code);

            var result = await CronExpressionQuickInfoSource.CalculateQuickInfoAsync(document, position, CancellationToken.None);

            Assert.That(result.HasValue);
            Assert.That(result.Value.message, Is.Not.Null);
            Assert.That(result.Value.message.Count, Is.EqualTo(2));
            Assert.That(result.Value.message.First(), Is.EqualTo("Every minute"));
        }

        [Test]
        public async Task CanFindExpressionInMethodAttribute()
        {
            var code = @"
public class Test
{
    [Trigger(""* * *¤ * *"")]
    public void Method1(string expression)
    {
    }
}";

            var position = code.IndexOf("¤");
            var document = Document(code);

            var result = await CronExpressionQuickInfoSource.CalculateQuickInfoAsync(document, position, CancellationToken.None);

            Assert.That(result.HasValue);
            Assert.That(result.Value.message, Is.Not.Null);
            Assert.That(result.Value.message.Count, Is.EqualTo(2));
            Assert.That(result.Value.message.First(), Is.EqualTo("Every minute"));
        }

        [Test]
        public async Task CanFindExpressionInClassAttribute()
        {
            var code = @"
[Trigger(""* * *¤ * *"")]
public class Test
{
}";

            var position = code.IndexOf("¤");
            var document = Document(code);

            var result = await CronExpressionQuickInfoSource.CalculateQuickInfoAsync(document, position, CancellationToken.None);

            Assert.That(result.HasValue);
            Assert.That(result.Value.message, Is.Not.Null);
            Assert.That(result.Value.message.Count, Is.EqualTo(2));
            Assert.That(result.Value.message.First(), Is.EqualTo("Every minute"));
        }

        [Test]
        public async Task CanFindExpressionInVariable()
        {
            var code = @"
public class Test
{
    private string expression = ""* * *¤ * *"";
}";

            var position = code.IndexOf("¤");
            var document = Document(code);

            var result = await CronExpressionQuickInfoSource.CalculateQuickInfoAsync(document, position, CancellationToken.None);

            Assert.That(result.HasValue);
            Assert.That(result.Value.message, Is.Not.Null);
            Assert.That(result.Value.message.Count, Is.EqualTo(2));
            Assert.That(result.Value.message.First(), Is.EqualTo("Every minute"));
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

using DotnetLegacyMigrator.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System.IO;
using System.Linq;

namespace Translation.Tests;

public class NewResolveRewriterTests
{
    private static string ExpectedPath(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    private static string Normalize(string input) => input.Replace("\r\n", "\n").Trim();

    [Fact]
    public async Task NewAndResolveRewriters_ProduceConstructorInjection_WithoutModifyingDocument()
    {
        // Sample business code that mixes direct instantiation and a service locator
        var code = @"public class OrderTasks
{
    public void Run()
    {
        var processor = new PaymentProcessor();
        var facade = Globals.IoCContainer.Resolve<IPaymentFacade>();
        var config = _resolver.Resolve<OrderConfig>();
        var sender = new EmailSender();
    }
}";

        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = workspace.AddDocument(project.Id, "OrderTasks.cs", SourceText.From(code));

        var original = await document.GetTextAsync();

        // Run the pipeline and capture the transformed output
        var actual = Normalize(await RewriterPipeline.ApplyAsync(document));
        var expected = Normalize(File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "Rewriters", "OrderTasks.txt")));

        Assert.Equal(expected, actual);

        // Ensure the document in the workspace was not modified
        var after = await document.GetTextAsync();
        Assert.Equal(original.ToString(), after.ToString());
    }
}

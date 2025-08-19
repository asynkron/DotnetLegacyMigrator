using DotnetLegacyMigrator.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    public void NewAndResolveRewriters_ProduceConstructorInjection()
    {
        // Sample business code that mixes direct instantiation and a service locator
        var code = @"
public class OrderTasks
{
    public void Run()
    {
        var processor = new PaymentProcessor();
        var facade = Globals.IoCContainer.Resolve<IPaymentFacade>();
        var config = _resolver.Resolve<OrderConfig>();
        var sender = new EmailSender();
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        // Replace "new" and service locator calls, then inject via ctor
        root = new NewRewriter().Visit(root);
        root = new ResolveRewriter().Visit(root);
        root = new CtorInjectRewriter().Visit(root);

        var actual = Normalize(root.NormalizeWhitespace().ToFullString());
        var expected = Normalize(File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "Rewriters", "OrderTasks.txt")));

        Assert.Equal(expected, actual);
    }
}

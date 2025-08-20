using DotnetLegacyMigrator;
using DotnetLegacyMigrator.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Linq;
using Xunit;

namespace Translation.Tests;

public class TypedDatasetKeyTests
{
    [Fact]
    public void TypedDataSet_CompositeKeys_And_IdentityColumns_AreHandled()
    {
        var designer = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "TypedDataSets", "KeyScenarios", "KeyDataSet.Designer.cs"));
        var xsd = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "TypedDataSets", "KeyScenarios", "KeyDataSet.xsd"));
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var designerPath = Path.Combine(dir, "KeyDataSet.Designer.cs");
        var xsdPath = Path.Combine(dir, "KeyDataSet.xsd");
        File.WriteAllText(designerPath, designer);
        File.WriteAllText(xsdPath, xsd);
        var tree = CSharpSyntaxTree.ParseText(designer, path: designerPath);
        var walker = new TypedDatasetEntitySyntaxWalker();
        walker.Visit(tree.GetRoot());
        var entityText = CodeGenerator.GenerateEntities(walker.Entities);
        var configText = CodeGenerator.GenerateEntityConfigurations(walker.Entities);
        var expectedEntities = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "TypedDataSets", "KeyScenarios", "Entities.txt"));
        var expectedConfig = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "TypedDataSets", "KeyScenarios", "EntityConfigurations.txt"));
        Assert.Equal(Normalize(expectedEntities), Normalize(entityText));
        Assert.Equal(Normalize(expectedConfig), Normalize(configText));
        var composite = walker.Entities.Single(e => e.Name == "CompositeTable");
        Assert.Equal(new[] { "KeyPart1", "KeyPart2" }, composite.Properties.Where(p => p.IsPrimaryKey).Select(p => p.Name).ToArray());
        var identity = walker.Entities.Single(e => e.Name == "IdentityTable");
        Assert.True(identity.Properties.Single(p => p.Name == "Id").IsDbGenerated);
    }

    private static string ExpectedPath(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    private static string Normalize(string input) => input.Replace("\r\n", "\n").Trim();
}

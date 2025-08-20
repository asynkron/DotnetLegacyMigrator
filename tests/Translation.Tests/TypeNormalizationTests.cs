using DotnetLegacyMigrator;
using DotnetLegacyMigrator.Models;
using Xunit;

namespace Translation.Tests;

public class TypeNormalizationTests
{
    [Fact]
    public void GeneratesAliasesAndUsingsForFullyQualifiedTypes()
    {
        var entity = new Entity
        {
            Name = "Doc",
            TableName = "Docs",
            Properties = new List<EntityProperty>
            {
                new()
                {
                    Name = "Id",
                    Type = "System.Int32",
                    IsPrimaryKey = true,
                    ColumnName = "Id",
                    IsNullable = false
                },
                new()
                {
                    Name = "Content",
                    Type = "System.Xml.Linq.XElement?",
                    ColumnName = "Content",
                    DbType = "XML",
                    IsNullable = true
                }
            }
        };

        var context = new DataContext
        {
            Name = "DemoContext",
            Tables =
            {
                new TableMapping { Name = "Docs", EntityType = "Doc" }
            }
        };

        var entityText = CodeGenerator.GenerateEntities(new[] { entity });
        var configText = CodeGenerator.GenerateEntityConfigurations(new[] { entity });
        var contextText = CodeGenerator.GenerateDataContext(context);

        var expectedEntity = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "TypeNormalization", "Entities.txt"));
        var expectedConfig = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "TypeNormalization", "EntityConfigurations.txt"));
        var expectedContext = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "TypeNormalization", "DataContext.txt"));

        Assert.Equal(Normalize(expectedEntity), Normalize(entityText));
        Assert.Equal(Normalize(expectedConfig), Normalize(configText));
        Assert.Equal(Normalize(expectedContext), Normalize(contextText));
    }

    private static string ExpectedPath(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    private static string Normalize(string input) => input.Replace("\r\n", "\n").Trim();
}

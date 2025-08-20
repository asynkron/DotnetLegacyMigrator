using DotnetLegacyMigrator;
using DotnetLegacyMigrator.Syntax;
using System.IO;
using Xunit;

namespace Translation.Tests;

public class NHibernateCompositeIdTests
{
    [Fact]
    public void NHibernate_CompositeId_ParsedAsCompositeKey()
    {
        // Load sample .hbm.xml with composite-id
        var hbm = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "NHibernate", "CompositeId", "OrderItem.hbm.xml"));
        var temp = Path.GetTempFileName();
        File.WriteAllText(temp, hbm);

        var (_, entities) = NHibernateHbmParser.ParseFiles(new[] { temp });
        var entityText = CodeGenerator.GenerateEntities(entities);
        var configText = CodeGenerator.GenerateEntityConfigurations(entities);

        var expectedEntities = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "NHibernate", "CompositeId", "Entities.txt"));
        var expectedConfig = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "NHibernate", "CompositeId", "EntityConfigurations.txt"));

        Assert.Equal(Normalize(expectedEntities), Normalize(entityText));
        Assert.Equal(Normalize(expectedConfig), Normalize(configText));
    }

    private static string ExpectedPath(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    private static string Normalize(string input) => input.Replace("\r\n", "\n").Trim();
}

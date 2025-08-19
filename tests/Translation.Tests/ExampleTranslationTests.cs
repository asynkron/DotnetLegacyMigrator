using DotnetLegacyMigrator;
using Xunit;

namespace Translation.Tests;

public class ExampleTranslationTests
{
    private static async Task<(string DataContext, string Entities)> GenerateAsync(string solution)
    {
        var (contexts, entities) = await MetadataCollector.CollectAsync(solution);
        var ctxText = CodeGenerator.GenerateDataContext(contexts.Single());
        var entityText = CodeGenerator.GenerateEntities(entities);
        return (ctxText, entityText);
    }

    private static string ExpectedPath(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    [Fact]
    public async Task LinqToSql_ProjectProducesExpectedOutput()
    {
        var sol = ExpectedPath("examples", "LinqToSql", "LinqToSqlDemo.sln");
        var (dataCtx, entities) = await GenerateAsync(sol);
        var expectedCtx = await File.ReadAllTextAsync(ExpectedPath("tests", "Translation.Tests", "Expected", "LinqToSql", "DataContext.txt"));
        var expectedEnt = await File.ReadAllTextAsync(ExpectedPath("tests", "Translation.Tests", "Expected", "LinqToSql", "Entities.txt"));
        Assert.Equal(Normalize(expectedCtx), Normalize(dataCtx));
        Assert.Equal(Normalize(expectedEnt), Normalize(entities));
    }

    [Fact]
    public async Task TypedDataSet_ProjectProducesExpectedOutput()
    {
        var sol = ExpectedPath("examples", "TypedDataSets", "TypedDataSetDemo.sln");
        var (dataCtx, entities) = await GenerateAsync(sol);
        var expectedCtx = await File.ReadAllTextAsync(ExpectedPath("tests", "Translation.Tests", "Expected", "TypedDataSets", "DataContext.txt"));
        var expectedEnt = await File.ReadAllTextAsync(ExpectedPath("tests", "Translation.Tests", "Expected", "TypedDataSets", "Entities.txt"));
        Assert.Equal(Normalize(expectedCtx), Normalize(dataCtx));
        Assert.Equal(Normalize(expectedEnt), Normalize(entities));
    }

    [Fact]
    public async Task NHibernate_ProjectProducesExpectedOutput()
    {
        var sol = ExpectedPath("examples", "NHibernate", "NHibernateDemo.sln");
        var (dataCtx, entities) = await GenerateAsync(sol);
        var expectedCtx = await File.ReadAllTextAsync(ExpectedPath("tests", "Translation.Tests", "Expected", "NHibernate", "DataContext.txt"));
        var expectedEnt = await File.ReadAllTextAsync(ExpectedPath("tests", "Translation.Tests", "Expected", "NHibernate", "Entities.txt"));
        Assert.Equal(Normalize(expectedCtx), Normalize(dataCtx));
        Assert.Equal(Normalize(expectedEnt), Normalize(entities));
    }

    private static string Normalize(string input) => input.Replace("\r\n", "\n").Trim();
}

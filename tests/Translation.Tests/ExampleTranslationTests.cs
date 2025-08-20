using DotnetLegacyMigrator;
using Xunit;

namespace Translation.Tests;

public class ExampleTranslationTests
{
    private static async Task<(string DataContext, string Entities, string Results)> GenerateAsync(string solution)
    {
        var (contexts, entities, results) = await MetadataCollector.CollectAsync(solution);
        var ctxText = CodeGenerator.GenerateDataContext(contexts.Single());
        var entityText = CodeGenerator.GenerateEntities(entities);
        var resultText = CodeGenerator.GenerateStoredProcedureResults(results);
        return (ctxText, entityText, resultText);
    }

    private static string ExpectedPath(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    private static async Task<string> ReadExpectedAsync(params string[] parts) =>
        File.Exists(ExpectedPath(parts))
            ? await File.ReadAllTextAsync(ExpectedPath(parts))
            : string.Empty;

    [Fact]
    public async Task LinqToSql_ProjectProducesExpectedOutput()
    {
        var sol = ExpectedPath("examples", "LinqToSql", "LinqToSqlDemo.sln");
        var (dataCtx, entities, results) = await GenerateAsync(sol);
        var expectedCtx = await ReadExpectedAsync("tests", "Translation.Tests", "Expected", "LinqToSql", "DataContext.txt");
        var expectedEnt = await ReadExpectedAsync("tests", "Translation.Tests", "Expected", "LinqToSql", "Entities.txt");
        var expectedRes = await ReadExpectedAsync("tests", "Translation.Tests", "Expected", "LinqToSql", "StoredProcedureResults.txt");
        Assert.Equal(Normalize(expectedCtx), Normalize(dataCtx));
        Assert.Equal(Normalize(expectedEnt), Normalize(entities));
        Assert.Equal(Normalize(expectedRes), Normalize(results));
    }

    [Fact]
    public async Task TypedDataSet_ProjectProducesExpectedOutput()
    {
        var sol = ExpectedPath("examples", "TypedDataSets", "TypedDataSetDemo.sln");
        var (dataCtx, entities, results) = await GenerateAsync(sol);
        var expectedCtx = await ReadExpectedAsync("tests", "Translation.Tests", "Expected", "TypedDataSets", "DataContext.txt");
        var expectedEnt = await ReadExpectedAsync("tests", "Translation.Tests", "Expected", "TypedDataSets", "Entities.txt");
        var expectedRes = await ReadExpectedAsync("tests", "Translation.Tests", "Expected", "TypedDataSets", "StoredProcedureResults.txt");
        Assert.Equal(Normalize(expectedCtx), Normalize(dataCtx));
        Assert.Equal(Normalize(expectedEnt), Normalize(entities));
        Assert.Equal(Normalize(expectedRes), Normalize(results));
    }

    [Fact]
    public async Task NHibernate_ProjectProducesExpectedOutput()
    {
        var sol = ExpectedPath("examples", "NHibernate", "NHibernateDemo.sln");
        var (dataCtx, entities, results) = await GenerateAsync(sol);
        var expectedCtx = await ReadExpectedAsync("tests", "Translation.Tests", "Expected", "NHibernate", "DataContext.txt");
        var expectedEnt = await ReadExpectedAsync("tests", "Translation.Tests", "Expected", "NHibernate", "Entities.txt");
        var expectedRes = await ReadExpectedAsync("tests", "Translation.Tests", "Expected", "NHibernate", "StoredProcedureResults.txt");
        Assert.Equal(Normalize(expectedCtx), Normalize(dataCtx));
        Assert.Equal(Normalize(expectedEnt), Normalize(entities));
        Assert.Equal(Normalize(expectedRes), Normalize(results));
    }

    private static string Normalize(string input) => input.Replace("\r\n", "\n").Trim();
}

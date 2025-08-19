using DotnetLegacyMigrator;
using DotnetLegacyMigrator.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using Xunit;

namespace Translation.Tests;

public class RelationshipMigrationTests
{
    [Fact]
    public void LinqToSql_OneToMany_NavigationGenerated()
    {
        var code = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "Relationships", "LinqToSql", "Model.cs.txt"));
        var tree = CSharpSyntaxTree.ParseText(code);
        var walker = new LinqToSqlEntitySyntaxWalker();
        walker.Visit(tree.GetRoot());
        var output = CodeGenerator.GenerateEntities(walker.Entities);
        var expected = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "Relationships", "LinqToSql", "Entities.txt"));
        Assert.Equal(Normalize(expected), Normalize(output));
    }

    [Fact]
    public void NHibernate_ManyToMany_NavigationGenerated()
    {
        var studentHbm = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "Relationships", "NHibernate", "Student.hbm.xml"));
        var courseHbm = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "Relationships", "NHibernate", "Course.hbm.xml"));
        var f1 = Path.GetTempFileName();
        var f2 = Path.GetTempFileName();
        File.WriteAllText(f1, studentHbm);
        File.WriteAllText(f2, courseHbm);
        var (_, entities) = NHibernateHbmParser.ParseFiles(new[] { f1, f2 });
        var output = CodeGenerator.GenerateEntities(entities);
        var expected = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "Relationships", "NHibernate", "Entities.txt"));
        Assert.Equal(Normalize(expected), Normalize(output));
    }

    private static string ExpectedPath(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    private static string Normalize(string input) => input.Replace("\r\n", "\n").Trim();
}


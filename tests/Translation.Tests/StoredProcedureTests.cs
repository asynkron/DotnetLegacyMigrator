using DotnetLegacyMigrator;
using DotnetLegacyMigrator.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Translation.Tests;

public class StoredProcedureTests
{
    private static string ExpectedPath(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    private static string Normalize(string input) => input.Replace("\r\n", "\n").Trim();

    [Fact]
    public void CapturesParameterDirectionAndSize()
    {
        const string source = """
using System.Data.Linq;
using System.Data.Linq.Mapping;

public class MyContext : DataContext
{
    [Function(Name="dbo.MyProc")]
    public int MyProc(
        [Parameter(DbType="NVarChar(50)")] string inParam,
        [Parameter(DbType="NVarChar(100)")] ref string inOutParam,
        [Parameter(DbType="Int")] out int outParam)
    {
        outParam = 0;
        return 0;
    }
}
""";
        var tree = CSharpSyntaxTree.ParseText(source);
        var walker = new LinqToSqlContextSyntaxWalker();
        walker.Visit(tree.GetRoot());
        var context = Assert.Single(walker.Contexts);
        var sp = Assert.Single(context.StoredProcedures);
        Assert.Equal("MyProc", sp.MethodName);
        Assert.Collection(sp.Parameters,
            p =>
            {
                Assert.Equal("inParam", p.Name);
                Assert.Equal("Input", p.Direction);
                Assert.Equal(50, p.Size);
            },
            p =>
            {
                Assert.Equal("inOutParam", p.Name);
                Assert.Equal("InputOutput", p.Direction);
                Assert.Equal(100, p.Size);
            },
            p =>
            {
                Assert.Equal("outParam", p.Name);
                Assert.Equal("Output", p.Direction);
                Assert.Null(p.Size);
            });

        var ctxText = CodeGenerator.GenerateDataContext(context);
        var expected = File.ReadAllText(ExpectedPath("tests", "Translation.Tests", "Expected", "LinqToSql", "StoredProcWithOutput.txt"));
        Assert.Equal(Normalize(expected), Normalize(ctxText));
    }
}

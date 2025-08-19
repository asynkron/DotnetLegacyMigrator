using DotnetLegacyMigrator.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Translation.Tests;

public class LinqToSqlContextSyntaxWalkerTests
{
    [Fact]
    public void CollectsTablesFromPropertiesAndFields()
    {
        const string source = """
using System.Data.Linq;

public class MyContext : DataContext
{
    public Table<Customer> Customers { get; }
    public Table<Order> Orders;
}
""";

        var tree = CSharpSyntaxTree.ParseText(source);
        var walker = new LinqToSqlContextSyntaxWalker();
        walker.Visit(tree.GetRoot());

        var context = Assert.Single(walker.Contexts);
        Assert.Collection(context.Tables,
            t =>
            {
                Assert.Equal("Customers", t.Name);
                Assert.Equal("Customer", t.EntityType);
            },
            t =>
            {
                Assert.Equal("Orders", t.Name);
                Assert.Equal("Order", t.EntityType);
            });
    }
}

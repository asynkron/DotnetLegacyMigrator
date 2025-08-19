using DotnetLegacyMigrator;
using DotnetLegacyMigrator.Syntax;
using DotnetLegacyMigrator.Models;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using Xunit;

namespace Translation.Tests;

public class RelationshipMigrationTests
{
    [Fact]
    public void LinqToSql_OneToMany_NavigationGenerated()
    {
        var code = @"using System.Data.Linq;using System.Data.Linq.Mapping;[Table(Name=""Customers"")]public class Customer{[Column(IsPrimaryKey=true)]public int Id{get;set;}private EntitySet<Order> _Orders=new();[Association(Name=""FK_Orders_Customers"",ThisKey=""Id"",OtherKey=""CustomerId"")]public EntitySet<Order> Orders{get{return _Orders;}}}[Table(Name=""Orders"")]public class Order{[Column(IsPrimaryKey=true)]public int Id{get;set;}[Column]public int CustomerId{get;set;}private EntityRef<Customer> _Customer;[Association(Name=""FK_Orders_Customers"",ThisKey=""CustomerId"",OtherKey=""Id"",IsForeignKey=true)]public Customer Customer{get{return _Customer.Entity;}set{_Customer.Entity=value;}}}";
        var tree = CSharpSyntaxTree.ParseText(code);
        var walker = new LinqToSqlEntitySyntaxWalker();
        walker.Visit(tree.GetRoot());
        var output = CodeGenerator.GenerateEntities(walker.Entities);
        Assert.Contains("public List<Order> Orders { get; set; } = new();", output);
        Assert.Contains("[ForeignKey(\"CustomerId\")]", output);
    }

    [Fact]
    public void NHibernate_ManyToMany_NavigationGenerated()
    {
        var studentHbm = @"<hibernate-mapping xmlns=""urn:nhibernate-mapping-2.2""><class name=""Student"" table=""Students""><id name=""Id"" column=""Id"" type=""Int32""><generator class=""native""/></id><bag name=""Courses"" table=""StudentCourse""><key column=""StudentId""/><many-to-many class=""Course"" column=""CourseId""/></bag></class></hibernate-mapping>";
        var courseHbm = @"<hibernate-mapping xmlns=""urn:nhibernate-mapping-2.2""><class name=""Course"" table=""Courses""><id name=""Id"" column=""Id"" type=""Int32""><generator class=""native""/></id><bag name=""Students"" table=""StudentCourse""><key column=""CourseId""/><many-to-many class=""Student"" column=""StudentId""/></bag></class></hibernate-mapping>";
        var f1 = Path.GetTempFileName();
        var f2 = Path.GetTempFileName();
        File.WriteAllText(f1, studentHbm);
        File.WriteAllText(f2, courseHbm);
        var (_, entities) = NHibernateHbmParser.ParseFiles(new[] { f1, f2 });
        var output = CodeGenerator.GenerateEntities(entities);
        Assert.Contains("public List<Course> Courses { get; set; } = new();", output);
        Assert.Contains("public List<Student> Students { get; set; } = new();", output);
    }
}

using DotnetLegacyMigrator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Data;
using System.Xml;
using System.IO;

namespace DotnetLegacyMigrator.Syntax;

public class TypedDatasetEntitySyntaxWalker : CSharpSyntaxWalker
{
    public List<Entity> Entities { get; } = new List<Entity>();
    public List<StoredProcedureResult> StoredProcedureResults { get; } = new List<StoredProcedureResult>();
    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.BaseList != null && node.BaseList.Types
            .Any(t => t.Type.ToString().Contains("TypedTableBase") || t.Type.ToString().Contains("DataTable")))
        {
            var ds = LoadDataSet(node);
            if (ds == null)
            {
                base.VisitClassDeclaration(node);
                return;
            }

            var className = node.Identifier.ToString().Replace("DataTable", "");
            var tableName = ExtractTableName(node) ?? className;

            var dt = ds.Tables[tableName];
            if (dt != null)
            {
                var entity = new Entity
                {
                    Name = className,
                    TableName = tableName, // For Typed Datasets, the table name usually matches the class name
                    Properties = ExtractEntityProperties(dt).ToList()
                };

                AddRelations(ds, dt, entity);

                if (entity.Properties.Count > 0)
                {
                    //hack, make sure all tables has at least one key
                    if (!entity.Properties.Any(p => p.IsPrimaryKey))
                    {
                        entity.Properties.First().IsPrimaryKey = true;
                    }
                    Entities.Add(entity);
                }
            }
        }

        base.VisitClassDeclaration(node);
    }

    private static DataSet? LoadDataSet(ClassDeclarationSyntax node)
    {
        // Resolve the associated XSD file and load its schema into a DataSet
        var csFile = node.SyntaxTree.FilePath;
        if (string.IsNullOrEmpty(csFile))
            return null; // no file on disk (e.g. interactive)

        var xsdFile = Path.Combine(
            Path.GetDirectoryName(csFile)!,
            Path.GetFileNameWithoutExtension(csFile)
                .Replace(".Designer", "")
                + ".xsd"
        );

        if (!File.Exists(xsdFile))
            return null; // XSD file not found

        var ds = new DataSet();
        using var reader = XmlReader.Create(xsdFile);
        ds.ReadXmlSchema(reader);
        return ds;
    }

    private static void AddRelations(DataSet ds, DataTable dt, Entity entity)
    {
        // Populate navigation properties based on dataset relations
        foreach (DataRelation rel in ds.Relations)
        {
            if (rel.ParentTable == dt)
            {
                entity.Navigations.Add(new Navigation
                {
                    Name = rel.ChildTable.TableName + "s",
                    TargetEntity = rel.ChildTable.TableName,
                    IsCollection = true
                });
            }
            if (rel.ChildTable == dt)
            {
                entity.Navigations.Add(new Navigation
                {
                    Name = rel.ParentTable.TableName,
                    TargetEntity = rel.ParentTable.TableName,
                    ForeignKey = rel.ChildColumns.First().ColumnName,
                    IsCollection = false
                });
            }
        }
    }

    private static string? ExtractTableName(ClassDeclarationSyntax classNode)
    {
        // look for the ctor whose name matches the class
        var ctor = classNode.Members
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == classNode.Identifier.Text);

        if (ctor?.Body != null)
        {
            foreach (var stmt in ctor.Body.Statements
                                      .OfType<ExpressionStatementSyntax>())
            {
                // look for: this.TableName = "Foo";
                if (stmt.Expression is AssignmentExpressionSyntax assign
                 && assign.Left is MemberAccessExpressionSyntax left
                 && left.Expression is ThisExpressionSyntax
                 && left.Name.Identifier.Text == "TableName"
                 && assign.Right is LiteralExpressionSyntax literal
                 && literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    return literal.Token.ValueText.Trim();
                }
            }
        }

        // fallback to class name (or whatever default you prefer)
        return null;
    }



    private static IEnumerable<EntityProperty> ExtractEntityProperties(DataTable dt)
    {
        // Parse the DataColumn initializations in InitClass
        var primaryKeys = new HashSet<string>(dt.PrimaryKey.Select(pk => pk.ColumnName));
        foreach (DataColumn c in dt.Columns)
        {
            yield return new EntityProperty
            {
                Name = c.ColumnName,
                Type = GetColumnType(c),
                IsPrimaryKey = primaryKeys.Contains(c.ColumnName),
                IsDbGenerated = c.AutoIncrement,
                ColumnName = c.ColumnName,
                DbType = c.DataType == typeof(string) && c.MaxLength > 0 ? $"NVARCHAR({c.MaxLength})" : null,
                IsNullable = c.AllowDBNull
            };
        }
    }

    private static string GetColumnType(DataColumn c)
    {
        if (c.DataType == typeof(string))
            return c.DataType.Name + (c.AllowDBNull ? "?" : string.Empty);

        if (c.DataType.IsValueType)
        {
            return c.DataType.Name + (c.AllowDBNull ? "?" : "");
        }

        return c.DataType.Name;
    }

}

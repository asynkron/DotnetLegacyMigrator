using LinqToSqlMetadataExtractor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Data;
using System.Xml;
using static TypedDatasetMetadataExtractor.TypedDatasetEntitySyntaxWalker;

namespace TypedDatasetMetadataExtractor;

class TypedDatasetEntitySyntaxWalker : CSharpSyntaxWalker
{
    public List<Entity> Entities { get; } = new List<Entity>();
    public List<StoredProcedureResult> StoredProcedureResults { get; } = new List<StoredProcedureResult>();
    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {

        if (node.BaseList != null && node.BaseList.Types
            .Any(t => t.Type.ToString().Contains("TypedTableBase")))
        {
            // get the .cs file path
            var csFile = node.SyntaxTree.FilePath;
            if (string.IsNullOrEmpty(csFile))
                return;  // no file on disk (e.g. interactive), bail out

            // replace "Foo.Designer.cs" with "Foo.xsd"
            var xsdFile = Path.Combine(
                Path.GetDirectoryName(csFile)!,
                Path.GetFileNameWithoutExtension(csFile)
                    .Replace(".Designer", "")
                    + ".xsd"
            );

            var ds = new DataSet();
            using var reader = XmlReader.Create(xsdFile);
            ds.ReadXmlSchema(reader);

            var className = node.Identifier.ToString().Replace("DataTable", "");
            var tableName = ExtractTableName(node);

            var dt = ds.Tables[tableName];
            if (dt != null)
            {
                var entity = new Entity
                {
                    Name = className,
                    TableName = tableName, // For Typed Datasets, the table name usually matches the class name
                    Properties = ExtractEntityProperties(dt).ToList()
                };

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

    private string ExtractTableName(ClassDeclarationSyntax classNode)
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
        return classNode.Identifier.Text;
    }



    private IEnumerable<EntityProperty> ExtractEntityProperties(DataTable dt)
    {
        // Parse the DataColumn initializations in InitClass
        foreach (DataColumn c in dt.Columns)
        {

            yield return new EntityProperty
            {
                Name = c.ColumnName,
                Type = GetColumnType(c),
                Metadata = string.Empty, // Metadata extraction not relevant for Typed Datasets
                IsPrimaryKey = dt.PrimaryKey.Any(cc => cc == c) || dt.Columns.Count == 1, // This information is typically unavailable in the code
                IsDbGenerated = false, // Same as above
                ColumnName = c.ColumnName,
                DbType = null,
                Order = 0, // Order is irrelevant for columns in Typed Datasets
                MaxLength = c.MaxLength != -1 ? c.MaxLength : null, // Max length not specified in this format

                // Relationships are not typically encoded in Typed Dataset code
                ForeignKey = null,
                ForeignEntity = null,
                ForeignMany = false,
                SelfMany = false,
                CascadeDelete = false
            };
        }
    }

    private static string GetColumnType(DataColumn c)
    {
        //ignore nullability for strings until we upgrade C#
        if (c.DataType == typeof(string)) return c.DataType.Name;

        if (c.DataType.IsValueType)
        {
            return c.DataType.Name + (c.AllowDBNull ? "?" : "");
        }

        return c.DataType.Name;
    }

    private string? ExtractColumnName(ObjectCreationExpressionSyntax creation)
    {
        // First argument to DataColumn constructor is the column name
        var columnNameArgument = creation.ArgumentList?.Arguments.FirstOrDefault();
        return columnNameArgument?.Expression.ToString().Trim('"');
    }

    private string? ExtractColumnType(ObjectCreationExpressionSyntax creation)
    {
        // Second argument to DataColumn constructor is the type (typeof(...))
        var typeArgument = creation.ArgumentList?.Arguments.ElementAtOrDefault(1)?.Expression;
        if (typeArgument is TypeOfExpressionSyntax typeOfExpression)
        {
            return typeOfExpression.Type.ToString(); // Extract the actual type (e.g., "Guid", "string")
        }

        return null;
    }

}

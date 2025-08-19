using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using DotnetLegacyMigrator.Models;

namespace DotnetLegacyMigrator.Syntax;

public class LinqToSqlEntitySyntaxWalker : CSharpSyntaxWalker
{
    public List<Entity> Entities { get; } = new List<Entity>();
    public List<StoredProcedureResult> StoredProcedureResults { get; } = new List<StoredProcedureResult>();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var tableAttribute = node.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a => a.Name.ToString().Contains("TableAttribute"));

        if (tableAttribute != null)
        {
            var tableName = tableAttribute.ArgumentList.Arguments
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "Name")?.Expression.ToString().Trim('"');


            var entity = new Entity
            {
                Name = node.Identifier.ToString(),
                TableName = tableName ?? node.Identifier.ToString(),
                Properties = node.Members.OfType<PropertyDeclarationSyntax>()
                    .Select(GetEntityProperty).ToList()
            };
            Entities.Add(entity);
        }
        else if (IsStoredProcedureResult(node))
        {
            var result = new StoredProcedureResult
            {
                Name = node.Identifier.ToString(),
                Properties = node.Members.OfType<PropertyDeclarationSyntax>()
                    .Select((p, index) => new EntityProperty
                    {
                        Name = p.Identifier.ToString(),
                        Type = p.Type.ToString(),
                        Metadata = string.Join(", ", p.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .Select(a => a.ToString())),
                        ColumnName = p.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .Where(a => a.ToString().Contains("Storage="))
                            .Select(a => a.ArgumentList.Arguments
                                .FirstOrDefault(arg => arg.NameEquals.Name.Identifier.Text == "Storage")?.Expression
                                .ToString().Trim('"'))
                            .FirstOrDefault(),
                        DbType = p.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .Where(a => a.ToString().Contains("DbType="))
                            .Select(a => a.ArgumentList.Arguments
                                .FirstOrDefault(arg => arg.NameEquals.Name.Identifier.Text == "DbType")?.Expression
                                .ToString().Trim('"'))
                            .FirstOrDefault(),
                        IsNullable = p.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .Any(a => a.ToString().Contains("CanBeNull=true")),
                        Order = index + 1,
                        MaxLength = p.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .Where(a => a.ToString().Contains("NVarChar"))
                            .Select(ExtractMaxLength)
                            .FirstOrDefault(x => x != null)
                    }).ToList()
            };
            StoredProcedureResults.Add(result);
        }

        base.VisitClassDeclaration(node);
    }

    private EntityProperty GetEntityProperty(PropertyDeclarationSyntax p, int index)
    {
        return new EntityProperty
        {
            Name = p.Identifier.ToString(),
            Type = p.Type.ToString(),
            Metadata = GetMetadata(p),
            IsPrimaryKey = GetIsPrimaryKey(p),
            IsDbGenerated = GetIsDbGenerated(p),
            ColumnName = GetColumnName(p),
            DbType = GetDbType(p),
            Order = index + 1,
            MaxLength = GetMaxLength(p),

            // Extracting association properties
            ForeignKey = GetForeignKey(p),

            ForeignEntity = GetForeignEntity(p),

            ForeignMany = GetForeignMany(p),

            SelfMany = GetSelfMany(p),

            CascadeDelete = GetCascadeDelete(p)
        };
    }

    private int? GetMaxLength(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.ToString().Contains("NVarChar"))
            .Select(ExtractMaxLength)
            .FirstOrDefault(x => x != null);
    }

    private static string? GetDbType(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.ToString().Contains("DbType="))
            .Select(a => a.ArgumentList.Arguments
                .FirstOrDefault(arg => arg.NameEquals.Name.Identifier.Text == "DbType")?.Expression
                .ToString().Trim('"'))
            .FirstOrDefault();
    }

    private static string? GetColumnName(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.ToString().Contains("Name="))
            .Select(a => a.ArgumentList.Arguments
                .FirstOrDefault(arg => arg.NameEquals.Name.Identifier.Text == "Name")?.Expression
                .ToString().Trim('"'))
            .FirstOrDefault();
    }

    private static bool GetIsDbGenerated(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.ToString().Contains("IsDbGenerated=true"));
    }

    private static bool GetIsPrimaryKey(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.ToString().Contains("IsPrimaryKey=true"));
    }

    private static string GetMetadata(PropertyDeclarationSyntax p)
    {
        return string.Join(", ", p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Select(a => a.ToString()));
    }

    private static bool GetCascadeDelete(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.ToString().Contains("AssociationAttribute"))
            .Any(a => a.ArgumentList.Arguments
                .Any(arg => arg.NameEquals?.Name.Identifier.Text == "DeleteRule" &&
                            arg.Expression.ToString().Contains("CASCADE")));
    }

    private static bool GetSelfMany(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.ToString().Contains("AssociationAttribute"))
            .Any(a => a.ArgumentList.Arguments
                .Any(arg => arg.NameEquals?.Name.Identifier.Text == "IsForeignKey" &&
                            arg.Expression.ToString().Contains("true")));
    }

    private static bool GetForeignMany(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.ToString().Contains("AssociationAttribute"))
            .Any(a => a.ArgumentList.Arguments
                .Any(arg => arg.NameEquals?.Name.Identifier.Text == "IsForeignKey" &&
                            arg.Expression.ToString().Contains("false")));
    }

    private static string? GetForeignEntity(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.ToString().Contains("AssociationAttribute"))
            .Select(a => a.ArgumentList.Arguments
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "OtherKey")?.Expression
                .ToString().Trim('"'))
            .FirstOrDefault();
    }

    private static string? GetForeignKey(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.ToString().Contains("AssociationAttribute"))
            .Select(a => a.ArgumentList.Arguments
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "ThisKey")?.Expression
                .ToString().Trim('"'))
            .FirstOrDefault();
    }

    // Helper function to extract MaxLength from the attribute
    private int? ExtractMaxLength(AttributeSyntax maxLengthAttribute)
    {
        var argumentString = maxLengthAttribute.ToString();
        // Use regex to find the number inside parentheses after NVarChar
        var match = System.Text.RegularExpressions.Regex.Match(argumentString, @"NVarChar\((\d+)\)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var maxLength))
        {
            return maxLength;
        }

        return null;
    }

    private bool IsStoredProcedureResult(ClassDeclarationSyntax node)
    {
        // Heuristic: if the class name ends with "Result" and has no TableAttribute, we consider it a stored procedure result type
        return node.Identifier.ToString().EndsWith("Result") && !node.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("TableAttribute"));
    }
}
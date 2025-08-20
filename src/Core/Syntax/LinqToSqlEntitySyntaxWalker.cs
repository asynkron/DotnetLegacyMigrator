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
            .FirstOrDefault(a => SyntaxUtils.HasIdentifier(a, "Table"));

        var baseTypeName = node.BaseList?.Types.FirstOrDefault()?.Type.ToString();
        if (tableAttribute != null)
        {
            var tableName = tableAttribute.ArgumentList?.Arguments
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "Name")?
                .Expression?.ToString().Trim('"');
            var schemaName = tableAttribute.ArgumentList?.Arguments
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "Schema")?
                .Expression?.ToString().Trim('"');

            var (properties, navigations) = ParseMembers(node);

            var entity = new Entity
            {
                Name = node.Identifier.ToString(),
                BaseType = Entities.Any(e => e.Name == baseTypeName) ? baseTypeName : null,
                TableName = tableName ?? node.Identifier.ToString(),
                Schema = schemaName,
                Properties = properties,
                Navigations = navigations
            };
            Entities.Add(entity);
        }
        else if (baseTypeName != null && Entities.Any(e => e.Name == baseTypeName))
        {
            var baseEntity = Entities.First(e => e.Name == baseTypeName);
            var (properties, navigations) = ParseMembers(node);

            var entity = new Entity
            {
                Name = node.Identifier.ToString(),
                BaseType = baseTypeName,
                TableName = baseEntity.TableName,
                Schema = baseEntity.Schema,
                Properties = properties,
                Navigations = navigations
            };
            Entities.Add(entity);
        }
        else if (IsStoredProcedureResult(node))
        {
            var result = new StoredProcedureResult
            {
                Name = node.Identifier.ToString(),
                Properties = node.Members.OfType<PropertyDeclarationSyntax>()
                    .Select(p => new EntityProperty
                    {
                        Name = p.Identifier.ToString(),
                        Type = p.Type.ToString(),
                        ColumnName = p.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .Where(a => a.ToString().Contains("Storage="))
                            .Select(a => a.ArgumentList?.Arguments
                                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "Storage")?
                                .Expression?.ToString().Trim('"'))
                            .FirstOrDefault(),
                        DbType = p.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .Where(a => a.ToString().Contains("DbType="))
                            .Select(a => a.ArgumentList?.Arguments
                                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "DbType")?
                                .Expression?.ToString().Trim('"'))
                            .FirstOrDefault(),
                        IsNullable = GetIsNullable(p)
                    }).ToList()
            };
            StoredProcedureResults.Add(result);
        }

        base.VisitClassDeclaration(node);
    }

    // Parses both simple properties and navigation properties from the class.
    private (List<EntityProperty> properties, List<Navigation> navigations) ParseMembers(ClassDeclarationSyntax node)
    {
        var properties = new List<EntityProperty>();
        var navigations = new List<Navigation>();
        foreach (var prop in node.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (TryGetNavigation(prop, out var nav))
                navigations.Add(nav);
            else
                properties.Add(GetEntityProperty(prop));
        }

        return (properties, navigations);
    }

    private EntityProperty GetEntityProperty(PropertyDeclarationSyntax p)
    {
        return new EntityProperty
        {
            Name = p.Identifier.ToString(),
            Type = p.Type.ToString(),
            IsPrimaryKey = GetIsPrimaryKey(p),
            IsDbGenerated = GetIsDbGenerated(p),
            ColumnName = GetColumnName(p),
            DbType = GetDbType(p),
            IsNullable = GetIsNullable(p)
        };
    }

    private static string? GetDbType(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Select(a => a.ArgumentList?.Arguments
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "DbType")?
                .Expression?.ToString().Trim('"'))
            .FirstOrDefault(s => !string.IsNullOrEmpty(s));
    }

    private static string? GetColumnName(PropertyDeclarationSyntax p)
    {
        return p.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.ToString().Contains("Name="))
            .Select(a => a.ArgumentList?.Arguments
                .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "Name")?
                .Expression?.ToString().Trim('"'))
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
            .Any(a => a.ArgumentList?.Arguments
                .Any(arg => arg.NameEquals?.Name.Identifier.Text == "IsPrimaryKey" &&
                             arg.Expression.ToString().Contains("true")) ?? false);
    }

    private static bool GetIsNullable(PropertyDeclarationSyntax p)
    {
        return p.Type is NullableTypeSyntax ||
            p.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.ToString().Contains("CanBeNull=true"));
    }

    private bool IsStoredProcedureResult(ClassDeclarationSyntax node)
    {
        // Heuristic: if the class name ends with "Result" and has no TableAttribute, we consider it a stored procedure result type
        return node.Identifier.ToString().EndsWith("Result") && !node.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => SyntaxUtils.HasIdentifier(a, "Table"));
    }

    private bool TryGetNavigation(PropertyDeclarationSyntax p, out Navigation nav)
    {
        nav = default!;
        var assoc = p.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a => SyntaxUtils.HasIdentifier(a, "Association"));
        if (assoc == null)
            return false;

        var typeName = p.Type.ToString();
        bool isCollection = typeName.StartsWith("EntitySet<");
        string target = isCollection
            ? typeName.Substring("EntitySet<".Length).TrimEnd('>')
            : typeName.StartsWith("EntityRef<")
                ? typeName.Substring("EntityRef<".Length).TrimEnd('>')
                : typeName;

        var fk = assoc.ArgumentList?.Arguments
            .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "ThisKey")?
            .Expression?.ToString().Trim('"');
        var name = assoc.ArgumentList?.Arguments
            .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "Name")?
            .Expression?.ToString().Trim('"');

        nav = new Navigation
        {
            Name = p.Identifier.ToString(),
            TargetEntity = target,
            IsCollection = isCollection,
            ForeignKey = fk,
            AssociationName = name
        };
        return true;
    }
}
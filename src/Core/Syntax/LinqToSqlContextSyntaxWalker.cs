using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using DotnetLegacyMigrator.Models;

namespace DotnetLegacyMigrator.Syntax;

public class LinqToSqlContextSyntaxWalker : CSharpSyntaxWalker
{
    public List<DataContext> Contexts { get; } = new List<DataContext>();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.BaseList != null && node.BaseList.Types
                .Any(t => t.Type.ToString().Contains("DataContext")))
        {
            var context = new DataContext
            {
                Name = node.Identifier.ToString(),
                Tables = node.Members
                    .Where(IsTableMember)
                    .Select(m => new TableMapping
                    {
                        Name = m switch
                        {
                            PropertyDeclarationSyntax p => p.Identifier.ToString(),
                            FieldDeclarationSyntax f => f.Declaration.Variables.First().Identifier.ToString(),
                            _ => string.Empty
                        },
                        EntityType = GetEntityType(m)!
                    })
                    .ToList(),
                StoredProcedures = node.Members.OfType<MethodDeclarationSyntax>()
                    .Where(IsStoredProcedureMethod)
                    .Select(m => new StoredProcedureMapping
                    {
                        MethodName = GetSprocMethodName(m),
                        StoredProcName = GetStoredProcName(m),
                        ReturnType = GetReturnType(m),
                        Parameters = GetParameters(m)
                    }).ToList()
            };

            Contexts.Add(context);
        }

        base.VisitClassDeclaration(node);
    }

    private string GetStoredProcName(MethodDeclarationSyntax m)
    {
        var attribute = m.AttributeLists
            .SelectMany(a => a.Attributes)
            .First(a => a.Name.ToString().Contains("Function"));

        var nameArgument = attribute.ArgumentList?.Arguments
            .First(arg => arg.NameEquals?.Name.Identifier.Text == "Name");

        return nameArgument!.Expression.ToString().Trim('"');
    }

    private static string GetSprocMethodName(MethodDeclarationSyntax m)
    {
        return m.Identifier.ToString();
    }

    private static bool IsTableMember(MemberDeclarationSyntax member) =>
        TryGetTableEntityType(member, out _);

    private static string? GetEntityType(MemberDeclarationSyntax member) =>
        TryGetTableEntityType(member, out var entity) ? entity : null;

    private static bool TryGetTableEntityType(MemberDeclarationSyntax member, out string? entityType)
    {
        entityType = null;

        TypeSyntax? type = member switch
        {
            PropertyDeclarationSyntax p => p.Type,
            FieldDeclarationSyntax f => f.Declaration.Type,
            _ => null
        };

        if (type is null)
        {
            return false;
        }

        GenericNameSyntax? generic = type switch
        {
            QualifiedNameSyntax { Right: GenericNameSyntax g } => g,
            GenericNameSyntax g => g,
            _ => null
        };

        if (generic is { Identifier.Text: "Table", TypeArgumentList.Arguments.Count: 1 })
        {
            entityType = generic.TypeArgumentList.Arguments[0].ToString();
            return true;
        }

        return false;
    }

    private bool IsStoredProcedureMethod(MethodDeclarationSyntax method)
    {
        return method.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString().Contains("Function"));
    }

    private string GetReturnType(MethodDeclarationSyntax method)
    {
        return method.ReturnType.ToString();
    }

    private List<ParameterMapping> GetParameters(MethodDeclarationSyntax method)
    {
        return method.ParameterList.Parameters.Select(p => new ParameterMapping
        {
            Name = p.Identifier.ToString(),
            Type = p.Type.ToString(),
            IsNullable = p.Type is NullableTypeSyntax
        }).ToList();
    }
}
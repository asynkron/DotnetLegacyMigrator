using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LinqToSqlMetadataExtractor;

class LinqToSqlContextSyntaxWalker : CSharpSyntaxWalker
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
                Tables = node.Members.OfType<PropertyDeclarationSyntax>()
                    .Where(IsTableProperty)
                    .Select(p => new TableMapping
                    {
                        Name = p.Identifier.ToString(),
                        EntityType = GetEntityType(p)
                    }).ToList(),
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
            .First(a => a.Name.ToString().Contains("FunctionAttribute"));

        var nameArgument = attribute.ArgumentList?.Arguments
            .First(arg => arg.NameEquals?.Name.Identifier.Text == "Name");

        return nameArgument!.Expression.ToString().Trim('"');
    }

    private static string GetSprocMethodName(MethodDeclarationSyntax m)
    {
        return m.Identifier.ToString();
    }

    private bool IsTableProperty(PropertyDeclarationSyntax property)
    {
        var type = property.Type;
        if (type is QualifiedNameSyntax qualifiedName)
        {
            if (qualifiedName.Right is GenericNameSyntax { Identifier.Text: "Table", TypeArgumentList.Arguments.Count: 1 })
            {
                return true;
            }
        }
        else if (type is GenericNameSyntax genericName)
        {
            if (genericName.Identifier.Text == "Table" &&
                genericName.TypeArgumentList.Arguments.Count == 1)
            {
                return true;
            }
        }
        return false;
    }

    private string GetEntityType(PropertyDeclarationSyntax property)
    {
        var type = property.Type;
        if (type is QualifiedNameSyntax qualifiedName)
        {
            if (qualifiedName.Right is GenericNameSyntax genericName)
            {
                return genericName.TypeArgumentList.Arguments[0].ToString();
            }
        }
        else if (type is GenericNameSyntax genericName)
        {
            return genericName.TypeArgumentList.Arguments[0].ToString();
        }
        return null;
    }

    private bool IsStoredProcedureMethod(MethodDeclarationSyntax method)
    {
        return method.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString().Contains("FunctionAttribute"));
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
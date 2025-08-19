using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using CaseExtensions;
using RoslynToy;

public class ResolveRewriter : CSharpSyntaxRewriter
{
    private readonly Dictionary<string, string> _newTypesToFields = new();
    private readonly string _fieldPrefix = "_"; // You can change this based on your naming conventions

    public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.IsStaticMethodOrProperty())
        {
            // If it's in a static method, we leave the "new" expression unchanged
            return base.VisitInvocationExpression(node);
        }

        if (node.Expression is not MemberAccessExpressionSyntax memberAccessExpr)
        {
            return base.VisitInvocationExpression(node);
        }

        var s = memberAccessExpr.Expression.ToString();

        if (s != "Globals.IoCContainer" && s != "_resolver")
        {
            return base.VisitInvocationExpression(node);
        }

        if (!memberAccessExpr.Name.ToString().StartsWith("Resolve<"))
        {
            return base.VisitInvocationExpression(node);
        }

        var genericTypeArg = node.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault();
        if (genericTypeArg == null || !genericTypeArg.Identifier.Text.Equals("Resolve"))
        {
            return base.VisitInvocationExpression(node);
        }

        var actualTypeName = genericTypeArg.TypeArgumentList.Arguments[0].ToString();
        var typeArgument = genericTypeArg.TypeArgumentList.Arguments[0];

        // Extracting the non-generic type name
        var typeName = typeArgument is GenericNameSyntax genericName ? genericName.Identifier.Text : typeArgument.ToString();

        // Add a check to see if typeName ends with "Tasks" or "Config"
        if (!typeName.IsInjectableType())
        {
            return base.VisitInvocationExpression(node); // Return the original node if the condition isn't met
        }

        // If this is an interface, make sure we don't keep the "I" prefix
        if (typeName.Length > 1 && typeName[0] == 'I' && char.IsUpper(typeName[1]))
        {
            typeName = typeName[1..];
        }

        // Generate a field name for this type if it doesn't exist yet
        var proposedFieldName = $"{_fieldPrefix}{typeName.ToCamelCase()}";

        if (!_newTypesToFields.ContainsKey(typeName))
        {
            _newTypesToFields[actualTypeName] = proposedFieldName;
        }

        // Replace the `_resolver.Resolve<T>()` expression with an identifier to the corresponding field
        return SyntaxFactory.IdentifierName(_newTypesToFields[actualTypeName]);
    }

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (!node.ShouldProcess())
        {
            Console.WriteLine("Skipping type " + node.Identifier.Text);
            return node;
        }

        var fields = node.GetMemberFields();

        // Mapping existing field types to their names
        var existingFieldsMapping = fields
            .ToDictionary(
                fds => fds.Declaration.Type.ToString(),
                fds => fds.Declaration.Variables.First().Identifier.Text
            );

        foreach (var pair in existingFieldsMapping)
        {
            if (!_newTypesToFields.ContainsKey(pair.Key))
            {
                _newTypesToFields[pair.Key] = pair.Value;
            }
        }

        // Visit the children nodes to replace all the "new" expressions with identifiers
        node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

        // Collect existing field names in the class
        var existingFieldNames = node.Members
            .OfType<FieldDeclarationSyntax>()
            .SelectMany(fds => fds.Declaration.Variables)
            .Select(v => v.Identifier.Text)
            .ToHashSet();

        List<FieldDeclarationSyntax> fieldDeclarations = new List<FieldDeclarationSyntax>();

        // Create a field for each type that's been new'ed up
        foreach (var entry in _newTypesToFields)
        {
            var newType = entry.Key;
            var fieldName = entry.Value;

            // If a field with the same name or same type already exists, skip it
            if (existingFieldNames.Contains(fieldName))
            {
                continue;
            }

            // Create a variable declaration with just a type and name
            var variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(newType))
                .AddVariables(SyntaxFactory.VariableDeclarator(fieldName));

            // Create a field declaration using the variable declaration
            var fieldDeclaration = SyntaxFactory.FieldDeclaration(variableDeclaration)
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),// Adding the private modifier
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));  // Adding the readonly modifier

            fieldDeclarations.Add(fieldDeclaration);
        }

        // Insert the field declarations at the top of the class
        if (fieldDeclarations.Any())
        {
            node = node.WithMembers(node.Members.InsertRange(0, fieldDeclarations));
        }

        return node;
    }
}

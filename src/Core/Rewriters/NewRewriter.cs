using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using CaseExtensions;
using DotnetLegacyMigrator.Utilities;

namespace DotnetLegacyMigrator.Rewriters;

public class NewRewriter : CSharpSyntaxRewriter
{
    private readonly Dictionary<string, string> _newTypesToFields = new();
    private readonly string _fieldPrefix = "_"; // You can change this based on your naming conventions

    public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        if (node.IsStaticMethodOrProperty())
        {
            // If it's in a static method, we leave the "new" expression unchanged
            return node;
        }

        var typeName = node.Type.ToString();
        if (!typeName.IsInjectableType())
        {
            return node;
        }

        // Generate a field name for this type if it doesn't exist yet
        var proposedFieldName = $"{_fieldPrefix}{typeName.ToCamelCase()}";
        if (!_newTypesToFields.ContainsKey(typeName) && !_newTypesToFields.ContainsValue(proposedFieldName))
        {
            _newTypesToFields[typeName] = proposedFieldName;
        }

        // Replace the "new" expression with an identifier to the corresponding field
        return SyntaxFactory.IdentifierName(_newTypesToFields[typeName]);
    }

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (!node.ShouldProcess())
        {
            Console.WriteLine("Skipping type " + node.Identifier.Text);
            return node;
        }

        // Collect existing field names in the class
        var existingFieldNames = node.Members
            .OfType<FieldDeclarationSyntax>()
            .SelectMany(fds => fds.Declaration.Variables)
            .Select(v => v.Identifier.Text)
            .ToHashSet();

        List<FieldDeclarationSyntax> fieldDeclarations = new List<FieldDeclarationSyntax>();

        // Visit the children nodes to replace all the "new" expressions with identifiers
        node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

        // Create a field for each type that's been new'ed up
        foreach (var entry in _newTypesToFields)
        {
            var newType = entry.Key;
            var fieldName = entry.Value;

            // If a field with the same name already exists, skip it
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
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword), // Adding the private modifier
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)); // Adding the readonly modifier

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

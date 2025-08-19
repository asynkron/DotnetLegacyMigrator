using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using CaseExtensions;
using DotnetLegacyMigrator.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotnetLegacyMigrator.Rewriters;

public class NewRewriter : CSharpSyntaxRewriter
{
    // Tracks field names generated for each discovered type during a class visit.
    // Reassigned for every class to keep mappings scoped and support nested classes.
    private Dictionary<string, string> _newTypesToFields = new();
    private readonly string _fieldPrefix = "_"; // You can change this based on your naming conventions
    private readonly ILogger<NewRewriter> _logger;

    public NewRewriter(ILogger<NewRewriter>? logger = null)
    {
        _logger = logger ?? NullLogger<NewRewriter>.Instance;
    }

    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
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

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Capture current mappings so nested classes don't clobber outer state
        var previousMappings = _newTypesToFields;
        _newTypesToFields = new Dictionary<string, string>();

        try
        {
            if (!node.ShouldProcess())
            {
                _logger.LogDebug("Skipping type {TypeName}", node.Identifier.Text);
                return node;
            }

            // Collect existing field names in the class
            var existingFieldNames = node.Members
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(fds => fds.Declaration.Variables)
                .Select(v => v.Identifier.Text)
                .ToHashSet();

            List<FieldDeclarationSyntax> fieldDeclarations = new();

            // Visit the children nodes to replace all the "new" expressions with identifiers
            var visited = base.VisitClassDeclaration(node);
            if (visited is ClassDeclarationSyntax classNode)
            {
                node = classNode;
            }
            else
            {
                return visited;
            }

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
        finally
        {
            // Restore mappings so outer classes continue unaffected
            _newTypesToFields = previousMappings;
        }
    }
}

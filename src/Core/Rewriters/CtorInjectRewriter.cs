using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DotnetLegacyMigrator.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotnetLegacyMigrator.Rewriters;

public class CtorInjectRewriter : CSharpSyntaxRewriter
{
    private readonly ILogger<CtorInjectRewriter> _logger;

    public CtorInjectRewriter(ILogger<CtorInjectRewriter>? logger = null)
    {
        _logger = logger ?? NullLogger<CtorInjectRewriter>.Instance;
    }

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (!node.ShouldProcess())
        {
            _logger.LogDebug("Skipping type {TypeName}", node.Identifier.Text);
            return node;
        }

        // Get all non-static fields in the class
        var fields = node.GetMemberFields();

        if (fields.Any())
        {
            node = EnsureConstructorExists(node);
            var constructor = node.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            constructor = InjectMemberFields(constructor, fields);
            constructor = RemoveSelfAssignments(constructor);

            node = node.ReplaceNode(node.Members.OfType<ConstructorDeclarationSyntax>().First(), constructor);
        }

        return base.VisitClassDeclaration(node);
    }

    private static ClassDeclarationSyntax EnsureConstructorExists(ClassDeclarationSyntax node)
    {
        var constructor = node.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
        if (constructor != null) return node;
        // Create a default constructor if one doesn't exist
        constructor = SyntaxFactory.ConstructorDeclaration(node.Identifier.Text)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBody(SyntaxFactory.Block());

        // Determine where to insert the constructor so it appears after the last field.
        // Using LastOrDefault avoids exceptions when no fields exist.
        var lastField = node.Members.OfType<FieldDeclarationSyntax>().LastOrDefault();
        var insertIndex = lastField != null
            ? node.Members.IndexOf(lastField) + 1
            : 0; // If there are no fields, insert at the start of the member list

        // Insert the constructor at the calculated index
        node = node.WithMembers(node.Members.Insert(insertIndex, constructor));

        return node;
    }

    private static ConstructorDeclarationSyntax? RemoveSelfAssignments(ConstructorDeclarationSyntax constructor)
    {
        // Process the constructor to remove self-assignments
        var statementsToRemove = new List<StatementSyntax>();
        foreach (var statement in constructor.Body.Statements)
        {
            if (statement is ExpressionStatementSyntax exprStatement &&
                exprStatement.Expression is AssignmentExpressionSyntax assignment &&
                assignment.Left is IdentifierNameSyntax leftIdentifier &&
                assignment.Right is IdentifierNameSyntax rightIdentifier &&
                leftIdentifier.Identifier.Text == rightIdentifier.Identifier.Text)
            {
                // Add self-assignment statements to the removal list
                statementsToRemove.Add(statement);
            }
        }

        // Remove self-assignment statements from the constructor
        foreach (var statement in statementsToRemove)
        {
            constructor = constructor.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
        }

        return constructor;
    }


    private ConstructorDeclarationSyntax InjectMemberFields(ConstructorDeclarationSyntax constructor, List<FieldDeclarationSyntax> fields)
    {
        var parameters = new List<ParameterSyntax>();
        var statements = new List<StatementSyntax>();

        // Collect names of existing parameters to avoid duplicates
        var existingParameterNames = constructor.ParameterList.Parameters.Select(p => p.Identifier.Text).ToHashSet();

        foreach (var field in fields)
        {
            foreach (var variable in field.Declaration.Variables)
            {
                var parameterName = variable.Identifier.Text.Replace("_", "");

                // Only add the parameter if it doesn't already exist
                if (!existingParameterNames.Contains(parameterName))
                {
                    var parameterType = field.Declaration.Type;
                    parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName)).WithType(parameterType));
                    existingParameterNames.Add(parameterName);
                }

                // Check if the field is initialized from a constructor parameter
                bool isInitializedFromCtorParam = IsFieldInitializedFromCtorParam(constructor, variable.Identifier.Text);

                if (!isInitializedFromCtorParam)
                {
                    var assignmentStatement = SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(variable.Identifier),
                            SyntaxFactory.IdentifierName(parameterName)));

                    statements.Add(assignmentStatement);
                }
            }
        }

        // Add parameters to the constructor and statements to its body
        var parameterList = constructor.ParameterList.AddParameters(parameters.ToArray());
        var body = constructor.Body.AddStatements(statements.ToArray());

        // Return the updated constructor
        return constructor.WithParameterList(parameterList).WithBody(body);
    }

    // Helper method to check if a field is initialized from a constructor parameter
    private bool IsFieldInitializedFromCtorParam(ConstructorDeclarationSyntax constructor, string fieldName)
    {
        return constructor.Body.Statements
            .OfType<ExpressionStatementSyntax>()
            .Select(stmt => stmt.Expression)
            .OfType<AssignmentExpressionSyntax>()
            .Any(assignment => assignment.Left is IdentifierNameSyntax idName &&
                               idName.Identifier.Text == fieldName &&
                               assignment.Right is IdentifierNameSyntax rightName &&
                               constructor.ParameterList.Parameters.Any(p => p.Identifier.Text == rightName.Identifier.Text));
    }

}

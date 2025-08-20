using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetLegacyMigrator.Syntax;

internal static class SyntaxUtils
{
    public static string GetIdentifier(NameSyntax name) => name switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        QualifiedNameSyntax qualified => GetIdentifier(qualified.Right),
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
        _ => string.Empty
    };

    public static bool HasIdentifier(AttributeSyntax attribute, string expected)
    {
        var id = GetIdentifier(attribute.Name);
        return id == expected || id == $"{expected}Attribute";
    }

    public static bool HasIdentifier(TypeSyntax type, string expected) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.Text == expected,
        QualifiedNameSyntax qualified => GetIdentifier(qualified) == expected,
        GenericNameSyntax generic => generic.Identifier.Text == expected,
        _ => false
    };
}

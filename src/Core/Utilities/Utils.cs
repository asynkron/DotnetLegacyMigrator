using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace DotnetLegacyMigrator.Utilities
{
    public static class Utils
    {
        // Patterns used to determine whether a class name should be processed
        public static IEnumerable<Regex> AllowNamePatterns { get; set; } = new[]
        {
            new Regex("Tasks$", RegexOptions.Compiled),
            new Regex("Facade$", RegexOptions.Compiled),
            new Regex("Command$", RegexOptions.Compiled),
            new Regex("Processor", RegexOptions.Compiled)
        };

        public static IEnumerable<Regex> DenyNamePatterns { get; set; } = new[]
        {
            new Regex("^Configure", RegexOptions.Compiled),
            new Regex("Tests$", RegexOptions.Compiled)
        };

        public static List<FieldDeclarationSyntax> GetMemberFields(this ClassDeclarationSyntax self)
        {
            var fields = self.Members
                .OfType<FieldDeclarationSyntax>()
                .Where(f => !f.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
                .Where(f => f.Declaration.Type.ToString().IsInjectableType())
                .ToList();

            return fields;
        }

        public static List<PropertyDeclarationSyntax> GetMemberProperties(this ClassDeclarationSyntax self)
        {
            var properties = self.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => !p.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
                .Where(p => p.Type is IdentifierNameSyntax typeIdentifier &&
                            (typeIdentifier.Identifier.Text.EndsWith("Tasks") ||

                             typeIdentifier.Identifier.Text.EndsWith("Config")))
                .ToList();

            return properties;
        }

        public static bool HasProperties(this ClassDeclarationSyntax self)
        {
            return self.GetMemberProperties().Any();
        }

        /// <summary>
        /// Determines whether a class should be processed by migration rewriters.
        /// </summary>
        /// <param name="node">The class declaration to evaluate.</param>
        /// <returns><c>true</c> when the class meets the criteria for processing; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Classes with static constructors, properties, or names matching <see cref="DenyNamePatterns"/> are skipped.
        /// Inheritance from <c>IncomingCallHandler</c> or a name matching <see cref="AllowNamePatterns"/> allows processing.
        /// </remarks>
        public static bool ShouldProcess(this ClassDeclarationSyntax node)
        {
            // Skip processing when a static constructor is present
            var hasStaticConstructor = node.Members
                .OfType<ConstructorDeclarationSyntax>()
                .Any(constructor => constructor.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)));

            if (hasStaticConstructor)
            {
                return false;
            }

            // Skip processing if the class has properties
            if (node.HasProperties())
            {
                return false;
            }

            var name = node.Identifier.Text;

            // Deny patterns take precedence
            if (DenyNamePatterns.Any(pattern => pattern.IsMatch(name)))
            {
                return false;
            }

            var isInheritedFromIncomingCallHandler = node.BaseList?.Types.Any(t => t.ToString() == "IncomingCallHandler") ?? false;

            // Allow processing when inheritance or allow patterns match
            if (isInheritedFromIncomingCallHandler || AllowNamePatterns.Any(pattern => pattern.IsMatch(name)))
            {
                return true;
            }

            return false;
        }

        public static bool IsStaticMethodOrProperty(this ExpressionSyntax node)
        {
            var parentMethod = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (parentMethod != null && parentMethod.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)))
            {
                // If it's in a static method, we leave the "new" expression unchanged
                return true;
            }

            var parentProperty = node.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            if (parentProperty != null && parentProperty.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)))
            {
                // If it's in a static property, we leave the "new" expression unchanged
                return true;
            }

            return false;
        }

        public static bool IsInjectableType(this string typeName)
        {

            return typeName.EndsWith("Tasks") || 
                   typeName.EndsWith("Config") ||
                   typeName.EndsWith("Facade") ||
                   typeName.EndsWith("EmailSender") ||
                   typeName.EndsWith("FeatureClient") ||
                   typeName.Contains("Processor");
        }
    }
}

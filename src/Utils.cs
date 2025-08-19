using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynToy
{
    public static class Utils
    {
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

        public static bool ShouldProcess(this ClassDeclarationSyntax node)
        {
            if (node.Identifier.Text == "DmsTimeRegistrationTasks")
            {

            }
            // Check for a static constructor in the class
            var hasStaticConstructor = node.Members
                .OfType<ConstructorDeclarationSyntax>()
                .Any(constructor => constructor.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)));

            if (hasStaticConstructor)
            {
                // Skip processing this class if a static constructor is found
                return false;
            }

            if (node.HasProperties())
            {
                // Skip processing if the class has properties
                return false;
            }

            var isInheritedFromIncomingCallHandler = node.BaseList?.Types.Any(t => t.ToString() == "IncomingCallHandler") ?? false;

            if (node.Identifier.Text.StartsWith("Configure"))
            {
                return false;
            }

            if (node.Identifier.Text.EndsWith("Tests"))
            {
                return false;
            }

            if (isInheritedFromIncomingCallHandler ||
                node.Identifier.Text.EndsWith("Tasks") ||
                node.Identifier.Text.EndsWith("Facade") ||
                node.Identifier.Text.EndsWith("Command") ||
                node.Identifier.Text.Contains("Processor"))
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

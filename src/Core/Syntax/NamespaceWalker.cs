using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetLegacyMigrator.Syntax;

/// <summary>
/// Finds the namespace declaration for a syntax tree.
/// </summary>
    public class NamespaceWalker : CSharpSyntaxWalker
    {
        public string Namespace { get; private set; } = string.Empty;

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        Namespace = node.Name.ToString();
        base.VisitNamespaceDeclaration(node);
    }

    public override void VisitCompilationUnit(CompilationUnitSyntax node)
    {
        if (!node.Members.OfType<NamespaceDeclarationSyntax>().Any())
        {
            Namespace = string.Empty;
        }
        base.VisitCompilationUnit(node);
    }
}

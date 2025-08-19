using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynToy
{

    public class NamespaceWalker : CSharpSyntaxWalker
    {
        public string Namespace { get; private set; }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            Namespace = node.Name.ToString();
            base.VisitNamespaceDeclaration(node);
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            // Handle global namespace (no explicit namespace declaration)
            if (!node.Members.OfType<NamespaceDeclarationSyntax>().Any())
            {
                Namespace = string.Empty; // or some other representation of global namespace
            }
            base.VisitCompilationUnit(node);
        }
    }

}

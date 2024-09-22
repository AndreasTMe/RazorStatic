using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RazorStatic.SourceGen.Extensions;

internal static class SyntaxNodeExtensions
{
    public static bool IsTargetAttributeNode(this SyntaxNode node, string name) =>
        node is AttributeSyntax attributeNode && attributeNode.Name.ToString().Equals(name);

    public static string GetFullNamespace(this SyntaxNode? node)
    {
        var currentNamespace = string.Empty;

        while (true)
        {
            switch (node)
            {
                case null:
                    return currentNamespace;
                case NamespaceDeclarationSyntax namespaceDeclaration:
                    currentNamespace = currentNamespace.Length > 0
                        ? $"{namespaceDeclaration.Name}.{currentNamespace}"
                        : namespaceDeclaration.Name.ToString();

                    break;
                case FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclaration:
                    currentNamespace = currentNamespace.Length > 0
                        ? $"{fileScopedNamespaceDeclaration.Name}.{currentNamespace}"
                        : fileScopedNamespaceDeclaration.Name.ToString();

                    break;
            }

            node = node.Parent;
        }
    }
}
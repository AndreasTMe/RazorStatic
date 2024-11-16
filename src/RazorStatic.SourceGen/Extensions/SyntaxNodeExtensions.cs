using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using RazorStatic.SourceGen.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RazorStatic.SourceGen.Extensions;

internal static class SyntaxNodeExtensions
{
    public static bool IsTargetAttributeNode(this SyntaxNode node, string name) =>
        node is AttributeSyntax attributeNode && attributeNode.Name.ToString().Equals(name);

    public static AttributeMemberData GetAttributeMembers(this SyntaxNode node, SemanticModel semanticModel)
    {
        var attributeSyntax = (AttributeSyntax)node;
        var properties      = new Dictionary<string, string>();

        foreach (var argument in attributeSyntax.ArgumentList!.Arguments.Where(syntax => syntax.NameEquals is not null))
        {
            if (semanticModel.GetOperation(argument) is not ISimpleAssignmentOperation operation)
                continue;

            if (operation.Value.ConstantValue is { HasValue: true, Value: string value })
                properties[argument.NameEquals!.Name.ToString()] = value;
        }

        return new AttributeMemberData(properties.ToImmutableDictionary());
    }
}
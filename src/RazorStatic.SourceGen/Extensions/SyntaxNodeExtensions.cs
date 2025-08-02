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
    public static bool IsTargetAttributeNode(this SyntaxNode node, string name)
    {
        if (node is not CompilationUnitSyntax { AttributeLists.Count: > 0 } cux)
        {
            return false;
        }

        foreach (var attributeSyntax in cux.AttributeLists.SelectMany(static l => l.Attributes))
        {
            if (attributeSyntax.Name.ToString() == name)
            {
                return true;
            }
        }

        return false;
    }

    public static AttributeMembers GetAttributeMembers(
        this SyntaxNode node,
        SemanticModel semanticModel,
        string attributeName)
    {
        var attributeSyntaxes = new List<AttributeSyntax>();
        foreach (var attributeSyntax in
                 ((CompilationUnitSyntax)node).AttributeLists.SelectMany(static a => a.Attributes))
        {
            if (attributeSyntax.Name.ToString() == attributeName)
            {
                attributeSyntaxes.Add(attributeSyntax);
            }
        }

        if (attributeSyntaxes.Count == 0)
        {
            return new AttributeMembers([]);
        }

        var members = new List<AttributeMemberData>();
        foreach (var attributeSyntax in attributeSyntaxes)
        {
            var properties = new Dictionary<string, string>();

            foreach (var argument in
                     attributeSyntax.ArgumentList!.Arguments.Where(static s => s.NameEquals is not null))
            {
                if (semanticModel.GetOperation(argument) is not ISimpleAssignmentOperation operation)
                    continue;

                if (operation.Value.ConstantValue is { HasValue: true, Value: string value })
                {
                    properties[argument.NameEquals!.Name.ToString()] = value;
                }
                else
                {
                    var syntax = operation.Value.Syntax.ToFullString();
                    if (syntax == "null")
                        continue;

                    properties[argument.NameEquals!.Name.ToString()] = syntax;
                }
            }

            members.Add(new AttributeMemberData(properties.ToImmutableDictionary()));
        }

        return new AttributeMembers([..members]);
    }
}
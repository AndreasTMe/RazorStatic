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
        node is CompilationUnitSyntax { AttributeLists.Count: > 0 } cux
        && cux.AttributeLists.SelectMany(l => l.Attributes).Any(a => a.Name.ToString() == name);

    public static AttributeMemberData GetAttributeMembers(this SyntaxNode node,
                                                          SemanticModel semanticModel,
                                                          string attributeName)
    {
        var attributeSyntax = ((CompilationUnitSyntax)node)
                              .AttributeLists
                              .SelectMany(a => a.Attributes)
                              .FirstOrDefault(a => a.Name.ToString() == attributeName);

        if (attributeSyntax is null)
        {
            return new AttributeMemberData(ImmutableDictionary<string, string>.Empty);
        }

        var properties = new Dictionary<string, string>();

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
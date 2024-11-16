using Microsoft.CodeAnalysis;
using RazorStatic.SourceGen.Utilities;

namespace RazorStatic.SourceGen.Extensions;

internal static class IncrementalGeneratorInitializationContextExtensions
{
    public static IncrementalValuesProvider<AttributeMemberData> GetSyntaxProvider(
        this IncrementalGeneratorInitializationContext context,
        string attributeName) =>
        context.SyntaxProvider
               .CreateSyntaxProvider(
                   (node, _) => node.IsTargetAttributeNode(attributeName),
                   static (ctx, _) => ctx.Node.GetAttributeMembers(ctx.SemanticModel));
}
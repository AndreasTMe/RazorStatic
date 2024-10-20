using Microsoft.CodeAnalysis;
using RazorStatic.Shared.Attributes;
using RazorStatic.SourceGen.Utilities;
using System;

namespace RazorStatic.SourceGen.Extensions;

internal static class IncrementalGeneratorInitializationContextExtensions
{
    private static readonly string DirectoriesSetup = nameof(DirectoriesSetupAttribute)
        .Replace(nameof(Attribute), string.Empty);

    public static IncrementalValuesProvider<AttributeMemberData> GetDirectoriesSetupSyntaxProvider(
        this IncrementalGeneratorInitializationContext context) =>
        context.SyntaxProvider
               .CreateSyntaxProvider(
                   static (node, _) => node.IsTargetAttributeNode(DirectoriesSetup),
                   static (ctx, _) => ctx.Node.GetAttributeMembers(ctx.SemanticModel));
}
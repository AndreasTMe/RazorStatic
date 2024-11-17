using Microsoft.CodeAnalysis;
using RazorStatic.SourceGen.Extensions;
using RazorStatic.SourceGen.Pipelines;
using RazorStatic.SourceGen.Utilities;
using Capture = RazorStatic.SourceGen.Utilities.Capture;

namespace RazorStatic.SourceGen;

[Generator]
internal class RazorStaticGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.AddDirectoriesSetupAttribute();
        context.AddCollectionDefinitionAttribute();
        context.AddStaticContentAttribute();

        var csProjPipeline = context.AnalyzerConfigOptionsProvider.Select(GeneratorPipelines.ReadCsProjPipeline());

        var directoriesSetupPipeline = context.GetSyntaxProvider(Constants.Attributes.DirectoriesSetup.Name);
        context.RegisterSourceOutput(
            csProjPipeline.Combine(directoriesSetupPipeline.Collect()),
            GeneratorPipelines.ExecuteDirectoriesSetupPipeline);

        var staticContentDirectoriesSetupPipeline = context.GetSyntaxProvider(Constants.Attributes.StaticContent.Name);
        context.RegisterSourceOutput(
            csProjPipeline.Combine(staticContentDirectoriesSetupPipeline.Collect()),
            GeneratorPipelines.ExecuteDirectoriesSetupForStaticContentPipeline);

        var pagesStorePipeline = context.CompilationProvider
                                        .Select(static (compilation, _) => compilation.AssemblyName)
                                        .Combine(csProjPipeline)
                                        .Combine(directoriesSetupPipeline.Collect())
                                        .Select(
                                            static (combine, _) => new Capture(
                                                combine.Left.Right,
                                                combine.Left.Left,
                                                combine.Right.IsDefaultOrEmpty
                                                || combine.Right[0].MemberData.IsDefaultOrEmpty
                                                    ? default
                                                    : combine.Right[0].MemberData[0]));
        context.RegisterSourceOutput(pagesStorePipeline, GeneratorPipelines.ExecutePagesStorePipeline);

        var collectionDefinitionPipeline = context.GetSyntaxProvider(Constants.Attributes.CollectionDefinition.Name);
        var pageCollectionsPipeline = pagesStorePipeline.Combine(collectionDefinitionPipeline.Collect())
                                                        .Select(
                                                            static (combine, _) => new Capture(
                                                                combine.Left.Properties,
                                                                combine.Left.AssemblyName,
                                                                combine.Left.DirectorySetup,
                                                                combine.Right.IsDefaultOrEmpty
                                                                    ? default
                                                                    : combine.Right[0].MemberData));
        context.RegisterSourceOutput(pageCollectionsPipeline, GeneratorPipelines.ExecutePageCollectionsPipeline);
    }
}
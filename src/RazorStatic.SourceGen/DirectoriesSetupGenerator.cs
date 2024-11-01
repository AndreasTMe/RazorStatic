using Microsoft.CodeAnalysis;
using RazorStatic.Shared;
using RazorStatic.Shared.Attributes;
using RazorStatic.SourceGen.Extensions;
using RazorStatic.SourceGen.Utilities;
using System;
using System.IO;

namespace RazorStatic.SourceGen;

[Generator]
internal sealed class DirectoriesSetupGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configOptionsProvider = context.AnalyzerConfigOptionsProvider
                                           .Select(
                                               static (provider, _) =>
                                                   DirectoryUtils.ReadCsProj(provider.GlobalOptions));

        var directoriesSetupSyntaxProvider = context.GetDirectoriesSetupSyntaxProvider();

        var compilationProvider = context.CompilationProvider
                                         .Select(static (compilation, _) => compilation.AssemblyName)
                                         .Combine(configOptionsProvider)
                                         .Select(static (combine, _) => new Capture(combine.Right, combine.Left))
                                         .Combine(directoriesSetupSyntaxProvider.Collect())
                                         .Select(
                                             static (combine, _) => combine.Left with
                                             {
                                                 DirectorySetup = combine.Right.IsDefaultOrEmpty
                                                     ? default
                                                     : combine.Right[0]
                                             });

        context.RegisterSourceOutput(compilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext context, Capture capture)
    {
        if (string.IsNullOrWhiteSpace(capture.Properties.ProjectDir)
            || string.IsNullOrWhiteSpace(capture.AssemblyName)
            || capture.DirectorySetup == default)
            return;

        try
        {
            var pagesDir = capture.DirectorySetup.Properties.TryGetValue(
                nameof(DirectoriesSetupAttribute.Pages),
                out var pagesDirName)
                ? Path.Combine(capture.Properties.ProjectDir, pagesDirName)
                : string.Empty;

            var contentDir = capture.DirectorySetup.Properties.TryGetValue(
                nameof(DirectoriesSetupAttribute.Content),
                out var contentDirName)
                ? Path.Combine(capture.Properties.ProjectDir, contentDirName)
                : string.Empty;

            var staticDir = capture.DirectorySetup.Properties.TryGetValue(
                nameof(DirectoriesSetupAttribute.Static),
                out var staticDirName)
                ? Path.Combine(capture.Properties.ProjectDir, staticDirName)
                : string.Empty;

            const string className = $"RazorStatic_{nameof(IDirectoriesSetup)}_Impl";

            context.AddSource(
                $"{className}.g.cs",
                $$"""
                  using RazorStatic.Shared;

                  namespace RazorStatic.Shared
                  {
                      internal sealed class {{className}} : {{nameof(IDirectoriesSetup)}}
                      {
                  #nullable enable
                          public string {{nameof(IDirectoriesSetup.Pages)}} => @"{{pagesDir}}";
                          public string {{nameof(IDirectoriesSetup.Content)}} => @"{{contentDir}}";
                          public string {{nameof(IDirectoriesSetup.Static)}} => @"{{staticDir}}";
                  #nullable disable
                      }
                  }
                  """);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
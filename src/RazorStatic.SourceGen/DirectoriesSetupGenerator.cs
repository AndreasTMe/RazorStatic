using Microsoft.CodeAnalysis;
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

        var directoriesSetupSyntaxProvider = context.GetSyntaxProvider(Constants.Attributes.DirectoriesSetup.Name);

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
                Constants.Attributes.DirectoriesSetup.Members.Pages,
                out var pagesDirName)
                ? Path.Combine(capture.Properties.ProjectDir!, pagesDirName)
                : string.Empty;

            var contentDir = capture.DirectorySetup.Properties.TryGetValue(
                Constants.Attributes.DirectoriesSetup.Members.Content,
                out var contentDirName)
                ? Path.Combine(capture.Properties.ProjectDir!, contentDirName)
                : string.Empty;

            var staticDir = capture.DirectorySetup.Properties.TryGetValue(
                Constants.Attributes.DirectoriesSetup.Members.Static,
                out var staticDirName)
                ? Path.Combine(capture.Properties.ProjectDir!, staticDirName)
                : string.Empty;

            const string className = $"RazorStatic_{Constants.Interfaces.DirectoriesSetup.Name}_Impl";

            context.AddSource(
                $"{className}.g.cs",
                $$"""
                  using {{Constants.Namespaces.RazorStatic}}.{{Constants.Namespaces.Abstractions}};

                  namespace {{Constants.Namespaces.RazorStatic}}.{{Constants.Namespaces.Core}}
                  {
                      internal sealed class {{className}} : {{Constants.Interfaces.DirectoriesSetup.Name}}
                      {
                  #nullable enable
                          public string {{Constants.Interfaces.DirectoriesSetup.Members.Pages}} => @"{{pagesDir}}";
                          public string {{Constants.Interfaces.DirectoriesSetup.Members.Content}} => @"{{contentDir}}";
                          public string {{Constants.Interfaces.DirectoriesSetup.Members.Static}} => @"{{staticDir}}";
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
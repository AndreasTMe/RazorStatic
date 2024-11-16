using Microsoft.CodeAnalysis;
using RazorStatic.SourceGen.Extensions;
using RazorStatic.SourceGen.Utilities;
using System;
using System.IO;
using System.Linq;

namespace RazorStatic.SourceGen;

[Generator]
internal sealed class PagesStoreGenerator : IIncrementalGenerator
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
            var pagesDir = Path.Combine(
                capture.Properties.ProjectDir!,
                capture.DirectorySetup.Properties[Constants.Attributes.DirectoriesSetup.Members.Pages]);
            var pages = Directory.GetFiles(pagesDir, "*.razor", SearchOption.AllDirectories);

            var typeMappings = pages.Select(pagePath => GetDirectoryToPageTypePair(pagePath, capture));

            const string className = $"RazorStatic_{Constants.Interfaces.PagesStore.Name}_Impl";

            context.AddSource(
                $"{className}.g.cs",
                $$"""
                  using Microsoft.AspNetCore.Components;
                  using Microsoft.AspNetCore.Components.Web;
                  using {{Constants.Namespaces.RazorStatic}}.{{Constants.Namespaces.Abstractions}};
                  using {{Constants.Namespaces.RazorStatic}}.{{Constants.Namespaces.Components}};
                  using System;
                  using System.Collections.Frozen;
                  using System.Collections.Generic;
                  using System.Threading.Tasks;

                  namespace {{Constants.Namespaces.RazorStatic}}.{{Constants.Namespaces.Core}}
                  {
                      internal sealed class {{className}} : {{Constants.Interfaces.PagesStore.Name}}
                      {
                  #nullable enable
                          private static readonly FrozenDictionary<string, Type> Types = new Dictionary<string, Type>()
                          {
                              {{string.Join(",\n            ", typeMappings)}}
                          }
                          .ToFrozenDictionary();
                          
                          private readonly HtmlRenderer _renderer;
                  
                          public {{className}}(HtmlRenderer renderer) => _renderer = renderer;
                          
                          public Type GetPageType(string filePath) => Types[filePath];
                  
                          public Task<string> {{Constants.Interfaces.PagesStore.Members.RenderComponentAsync}}(string filePath) => _renderer.Dispatcher.InvokeAsync(async () =>
                          {
                              var type = Types[filePath];
                              var parameters = type.IsSubclassOf(typeof({{Constants.Abstractions.FileComponentBase.Name}}))
                                  ? ParameterView.FromDictionary(new Dictionary<string, object?>
                                    {
                                        [nameof({{Constants.Abstractions.FileComponentBase.Name}}.{{Constants.Abstractions.FileComponentBase.Members.PageFilePath}})] = filePath,
                                    })
                                  : ParameterView.Empty;
                  
                              var output = await _renderer.RenderComponentAsync(type, parameters);
                              return output.ToHtmlString();
                          });
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

    private static string GetDirectoryToPageTypePair(string filePath, Capture capture) =>
        $"[@\"{filePath}\"] = {DirectoryUtils.GetPageType(filePath, capture.Properties.ProjectDir!, capture.AssemblyName!)}";
}
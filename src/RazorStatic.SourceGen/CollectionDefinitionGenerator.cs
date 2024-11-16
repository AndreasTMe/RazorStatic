using Microsoft.CodeAnalysis;
using RazorStatic.SourceGen.Extensions;
using RazorStatic.SourceGen.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Capture = RazorStatic.SourceGen.Utilities.Capture;

namespace RazorStatic.SourceGen;

[Generator]
internal class CollectionDefinitionGenerator : IIncrementalGenerator
{
    private const string PageRoute        = Constants.Attributes.CollectionDefinition.Members.PageRoute;
    private const string ContentDirectory = Constants.Attributes.CollectionDefinition.Members.ContentDirectory;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configOptionsProvider = context.AnalyzerConfigOptionsProvider
                                           .Select(
                                               static (provider, _) =>
                                                   DirectoryUtils.ReadCsProj(provider.GlobalOptions));

        var directoriesSetupSyntaxProvider = context.GetSyntaxProvider(Constants.Attributes.DirectoriesSetup.Name);
        var syntaxProvider                 = context.GetSyntaxProvider(Constants.Attributes.CollectionDefinition.Name);

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
                                             })
                                         .Combine(syntaxProvider.Collect())
                                         .Select(
                                             static (combine, _) => combine.Left with
                                             {
                                                 AttributeMembers = combine.Right
                                             });

        context.RegisterSourceOutput(compilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext context, Capture capture)
    {
        if (string.IsNullOrWhiteSpace(capture.Properties.ProjectDir)
            || string.IsNullOrWhiteSpace(capture.AssemblyName)
            || capture.AttributeMembers.IsDefaultOrEmpty)
            return;

        var pagesForFactory = new Dictionary<string, string>();
        var pagesDirName    = capture.DirectorySetup.Properties[Constants.Attributes.DirectoriesSetup.Members.Pages];
        var contentDirName  = capture.DirectorySetup.Properties[Constants.Attributes.DirectoriesSetup.Members.Content];

        foreach (var attributeInfo in capture.AttributeMembers.Where(
                     info => info.Properties.ContainsKey(PageRoute) && info.Properties.ContainsKey(ContentDirectory)))
        {
            try
            {
                var routeName = attributeInfo.Properties[PageRoute];
                var routeDir  = Path.Combine(capture.Properties.ProjectDir!, pagesDirName, routeName);
                var pageFile = Directory.GetFiles(routeDir, "*.razor", SearchOption.AllDirectories)
                                        .FirstOrDefault(
                                            file =>
                                            {
                                                var split             = file.Split(Path.DirectorySeparatorChar);
                                                var fileWithExtension = split[split.Length - 1];
                                                return fileWithExtension.StartsWith("[")
                                                       && fileWithExtension.EndsWith("].razor");
                                            });

                if (string.IsNullOrWhiteSpace(pageFile))
                    continue;

                var collectionDir = Path.Combine(
                    capture.Properties.ProjectDir!,
                    contentDirName,
                    attributeInfo.Properties[ContentDirectory]);
                var collectionRootDir = collectionDir[..collectionDir.LastIndexOf(Path.DirectorySeparatorChar)];
                var markdownFiles = Directory.GetFiles(collectionDir, "*.md", SearchOption.AllDirectories)
                                             .Select(file => $"@\"{file}\"");

                var routeNameNoSpecialChars = new Regex("[^a-zA-Z0-9_]").Replace(routeName, "");
                var className =
                    $"RazorStatic_{Constants.Interfaces.PageCollectionDefinition.Name}_ImplFor{routeNameNoSpecialChars}";

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
                          internal sealed class {{className}} : {{Constants.Interfaces.PageCollectionDefinition.Name}}
                          {
                      #nullable enable
                              private static readonly FrozenSet<string> ContentFilePaths = new HashSet<string>()
                              {
                                  {{string.Join(",\n            ", markdownFiles)}}
                              }
                              .ToFrozenSet();
                              
                              private readonly HtmlRenderer _renderer;
                              
                              public string {{Constants.Interfaces.PageCollectionDefinition.Members.RootPath}} => @"{{collectionRootDir}}";
                              
                              public {{className}}(HtmlRenderer renderer) => _renderer = renderer;
                      
                              public async IAsyncEnumerable<RenderedResult> {{Constants.Interfaces.PageCollectionDefinition.Members.RenderComponentsAsync}}(string filePath, Type pageType)
                              {
                                  foreach (var contentFilePath in ContentFilePaths)
                                  {
                                      var content = await _renderer.Dispatcher.InvokeAsync(async () =>
                                      {
                                          var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                                          {
                                              [nameof({{Constants.Abstractions.FileComponentBase.Name}}.{{Constants.Abstractions.FileComponentBase.Members.PageFilePath}})] = filePath,
                                              [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.ContentFilePath}})] = contentFilePath
                                          });
                                          var output = await _renderer.RenderComponentAsync(pageType, parameters);
                                          return output.ToHtmlString();
                                      });
                                      yield return new RenderedResult(contentFilePath, content);
                                  }
                              }
                      #nullable disable
                          }
                      }
                      """);

                pagesForFactory[pageFile] = className;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        context.AddSource(
            "RazorStatic_PageCollectionsStore.g.cs",
            $$"""
              using Microsoft.AspNetCore.Components.Web;
              using {{Constants.Namespaces.RazorStatic}}.{{Constants.Namespaces.Abstractions}};
              using System.Collections.Frozen;
              using System.Collections.Generic;
              using System.Diagnostics.CodeAnalysis;

              namespace {{Constants.Namespaces.RazorStatic}}.{{Constants.Namespaces.Core}}
              {
                  internal sealed class RazorStatic_PageCollectionsStore : {{Constants.Interfaces.PageCollectionsStore.Name}}
                  {
              #nullable enable
                      private readonly HtmlRenderer _renderer;
                      private readonly FrozenDictionary<string, {{Constants.Interfaces.PageCollectionDefinition.Name}}> _collections;
                      
                      public RazorStatic_PageCollectionsStore(HtmlRenderer renderer)
                      {
                          _renderer    = renderer;
                          _collections = new Dictionary<string, {{Constants.Interfaces.PageCollectionDefinition.Name}}>
                          {
                              {{string.Join(",\n            ", pagesForFactory.Select(kvp => $"[@\"{kvp.Key}\"] = new {kvp.Value}(renderer)"))}}
                          }
                          .ToFrozenDictionary();
                      }
                      
                      public bool {{Constants.Interfaces.PageCollectionsStore.Members.TryGetCollection}}(string key, [MaybeNullWhen(false)] out {{Constants.Interfaces.PageCollectionDefinition.Name}} collection)
                      {
                          return _collections.TryGetValue(key, out collection);
                      }
              #nullable disable
                  }
              }
              """);
    }
}
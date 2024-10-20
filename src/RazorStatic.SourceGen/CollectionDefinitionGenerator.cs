using Microsoft.CodeAnalysis;
using RazorStatic.Shared;
using RazorStatic.Shared.Attributes;
using RazorStatic.Shared.Components;
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
    private const string PageRoute        = nameof(CollectionDefinitionAttribute.PageRoute);
    private const string ContentDirectory = nameof(CollectionDefinitionAttribute.ContentDirectory);

    private static readonly string CollectionDefinition = nameof(CollectionDefinitionAttribute)
        .Replace(nameof(Attribute), string.Empty);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configOptionsProvider = context.AnalyzerConfigOptionsProvider
                                           .Select(
                                               static (provider, _) =>
                                                   DirectoryUtils.ReadCsProj(provider.GlobalOptions));

        var directoriesSetupSyntaxProvider = context.GetDirectoriesSetupSyntaxProvider();
        var syntaxProvider = context.SyntaxProvider
                                    .CreateSyntaxProvider(
                                        static (node, _) => node.IsTargetAttributeNode(CollectionDefinition),
                                        static (ctx, _) => ctx.Node.GetAttributeMembers(ctx.SemanticModel));

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
        var pagesDirName    = capture.DirectorySetup.Properties[nameof(DirectoriesSetupAttribute.Pages)];
        var contentDirName  = capture.DirectorySetup.Properties[nameof(DirectoriesSetupAttribute.Content)];

        foreach (var attributeInfo in capture.AttributeMembers.Where(
                     info => info.Properties.ContainsKey(PageRoute) && info.Properties.ContainsKey(ContentDirectory)))
        {
            try
            {
                var routeName = attributeInfo.Properties[PageRoute];
                var routeDir  = Path.Combine(capture.Properties.ProjectDir, pagesDirName, routeName);
                var pageFile = Directory.GetFiles(routeDir, "*.razor", SearchOption.AllDirectories)
                                        .FirstOrDefault(
                                            file =>
                                            {
                                                var fileWithExtension = file.Split(Path.DirectorySeparatorChar)[^1];
                                                return fileWithExtension.StartsWith('[')
                                                       && fileWithExtension.EndsWith("].razor");
                                            });

                if (string.IsNullOrWhiteSpace(pageFile))
                    continue;

                var collectionDir = Path.Combine(
                    capture.Properties.ProjectDir,
                    contentDirName,
                    attributeInfo.Properties[ContentDirectory]);
                var collectionRootDir = collectionDir[..collectionDir.LastIndexOf(Path.DirectorySeparatorChar)];
                var markdownFiles = Directory.GetFiles(collectionDir, "*.md", SearchOption.AllDirectories)
                                             .Select(file => $"@\"{file}\"");

                var routeNameNoSpecialChars = new Regex("[^a-zA-Z0-9_]").Replace(routeName, "");
                var className = $"RazorStatic_{nameof(IPageCollectionDefinition)}_ImplFor{routeNameNoSpecialChars}";

                context.AddSource(
                    $"{className}.g.cs",
                    $$"""
                      using Microsoft.AspNetCore.Components;
                      using Microsoft.AspNetCore.Components.Web;
                      using RazorStatic.Shared;
                      using RazorStatic.Shared.Components;
                      using System;
                      using System.Collections.Frozen;
                      using System.Collections.Generic;
                      using System.Threading.Tasks;

                      namespace RazorStatic.Shared
                      {
                          internal sealed class {{className}} : {{nameof(IPageCollectionDefinition)}}
                          {
                      #nullable enable
                              private static readonly FrozenSet<string> ContentFilePaths = new HashSet<string>()
                              {
                                  {{string.Join(",\n            ", markdownFiles)}}
                              }
                              .ToFrozenSet();
                              
                              private readonly HtmlRenderer _renderer;
                              
                              public string {{nameof(IPageCollectionDefinition.RootPath)}} => @"{{collectionRootDir}}";
                              
                              public {{className}}(HtmlRenderer renderer) => _renderer = renderer;
                      
                              public async IAsyncEnumerable<RenderedResult> {{nameof(IPageCollectionDefinition.RenderComponentsAsync)}}(string filePath, Type pageType)
                              {
                                  foreach (var contentFilePath in ContentFilePaths)
                                  {
                                      var content = await _renderer.Dispatcher.InvokeAsync(async () =>
                                      {
                                          var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                                          {
                                              [nameof({{nameof(FileComponentBase)}}.{{nameof(FileComponentBase.PageFilePath)}})] = filePath,
                                              [nameof({{nameof(CollectionFileComponentBase)}}.{{nameof(CollectionFileComponentBase.ContentFilePath)}})] = contentFilePath
                                          });
                                          var output = await _renderer.RenderComponentAsync(pageType, parameters).ConfigureAwait(false);
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
              using RazorStatic.Shared;
              using System.Collections.Frozen;
              using System.Collections.Generic;
              using System.Diagnostics.CodeAnalysis;

              namespace {{capture.AssemblyName}}
              {
                  internal sealed class RazorStatic_PageCollectionsStore : {{nameof(IPageCollectionsStore)}}
                  {
              #nullable enable
                      private readonly HtmlRenderer _renderer;
                      private readonly FrozenDictionary<string, {{nameof(IPageCollectionDefinition)}}> _collections;
                      
                      public RazorStatic_PageCollectionsStore(HtmlRenderer renderer)
                      {
                          _renderer    = renderer;
                          _collections = new Dictionary<string, {{nameof(IPageCollectionDefinition)}}>
                          {
                              {{string.Join(",\n            ", pagesForFactory.Select(kvp => $"[@\"{kvp.Key}\"] = new {kvp.Value}(renderer)"))}}
                          }
                          .ToFrozenDictionary();
                      }
                      
                      public bool {{nameof(IPageCollectionsStore.TryGetCollection)}}(string key, [MaybeNullWhen(false)] out {{nameof(IPageCollectionDefinition)}} collection)
                      {
                          return _collections.TryGetValue(key, out collection);
                      }
              #nullable disable
                  }
              }
              """);
    }
}
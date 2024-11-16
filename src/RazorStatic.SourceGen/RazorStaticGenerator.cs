﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RazorStatic.SourceGen.Extensions;
using RazorStatic.SourceGen.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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

        var csProjPipeline = context.AnalyzerConfigOptionsProvider.Select(ReadCsProj());

        var directoriesSetupPipeline = context.GetSyntaxProvider(Constants.Attributes.DirectoriesSetup.Name);
        context.RegisterSourceOutput(directoriesSetupPipeline.Combine(csProjPipeline), ExecuteDirectoriesSetupPipeline);

        var pagesStorePipeline = context.CompilationProvider
                                        .Select(static (compilation, _) => compilation.AssemblyName)
                                        .Combine(csProjPipeline)
                                        .Combine(directoriesSetupPipeline.Collect())
                                        .Select(
                                            static (combine, _) => new Capture(
                                                combine.Left.Right,
                                                combine.Left.Left,
                                                combine.Right.IsDefaultOrEmpty ? default : combine.Right[0]));
        context.RegisterSourceOutput(pagesStorePipeline, ExecutePagesStorePipeline);

        var collectionDefinitionPipeline = context.GetSyntaxProvider(Constants.Attributes.CollectionDefinition.Name);
        var pageCollectionsPipeline = pagesStorePipeline.Combine(collectionDefinitionPipeline.Collect())
                                                        .Select(
                                                            static (combine, _) => combine.Left with
                                                            {
                                                                AttributeMembers = combine.Right
                                                            });
        context.RegisterSourceOutput(pageCollectionsPipeline, ExecutePageCollectionsPipeline);
    }

    private static void ExecuteDirectoriesSetupPipeline(SourceProductionContext context,
                                                        (AttributeMemberData, CsProjProperties) source)
    {
        var (attribute, csProj) = source;

        var pagesDir = attribute.Properties.TryGetValue(
            Constants.Attributes.DirectoriesSetup.Members.Pages,
            out var pagesDirName)
            ? Path.Combine(csProj.ProjectDir, pagesDirName)
            : string.Empty;

        var contentDir = attribute.Properties.TryGetValue(
            Constants.Attributes.DirectoriesSetup.Members.Content,
            out var contentDirName)
            ? Path.Combine(csProj.ProjectDir, contentDirName)
            : string.Empty;

        var staticDir = attribute.Properties.TryGetValue(
            Constants.Attributes.DirectoriesSetup.Members.Static,
            out var staticDirName)
            ? Path.Combine(csProj.ProjectDir, staticDirName)
            : string.Empty;

        const string className = $"Implementations_{Constants.Interfaces.DirectoriesSetup.Name}";

        context.AddSource(
            $"{className}.generated.cs",
            $$"""
              // <auto-generated/>
              using {{Constants.RazorStaticAbstractionsNamespace}};

              namespace {{Constants.RazorStaticGeneratedNamespace}}
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

    private static void ExecutePagesStorePipeline(SourceProductionContext context, Capture capture)
    {
        if (string.IsNullOrWhiteSpace(capture.Properties.ProjectDir)
            || string.IsNullOrWhiteSpace(capture.AssemblyName)
            || capture.DirectorySetup == default)
            return;

        try
        {
            var pagesDir = Path.Combine(
                capture.Properties.ProjectDir,
                capture.DirectorySetup.Properties[Constants.Attributes.DirectoriesSetup.Members.Pages]);
            var pages = Directory.GetFiles(pagesDir, "*.razor", SearchOption.AllDirectories);

            var typeMappings = pages.Select(pagePath => GetDirectoryToPageTypePair(pagePath, capture));

            const string className = $"Implementations_{Constants.Interfaces.PagesStore.Name}";

            context.AddSource(
                $"{className}.generated.cs",
                $$"""
                  // <auto-generated/>
                  using Microsoft.AspNetCore.Components;
                  using Microsoft.AspNetCore.Components.Web;
                  using {{Constants.RazorStaticAbstractionsNamespace}};
                  using {{Constants.RazorStaticComponentsNamespace}};
                  using System;
                  using System.Collections.Frozen;
                  using System.Collections.Generic;
                  using System.Threading.Tasks;

                  namespace {{Constants.RazorStaticGeneratedNamespace}}
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

    private static void ExecutePageCollectionsPipeline(SourceProductionContext context, Capture capture)
    {
        if (string.IsNullOrWhiteSpace(capture.Properties.ProjectDir)
            || string.IsNullOrWhiteSpace(capture.AssemblyName)
            || capture.AttributeMembers.IsDefaultOrEmpty)
            return;

        var pagesForFactory = new Dictionary<string, string>();
        var pagesDirName    = capture.DirectorySetup.Properties[Constants.Attributes.DirectoriesSetup.Members.Pages];
        var contentDirName  = capture.DirectorySetup.Properties[Constants.Attributes.DirectoriesSetup.Members.Content];

        const string pageRoute        = Constants.Attributes.CollectionDefinition.Members.PageRoute;
        const string contentDirectory = Constants.Attributes.CollectionDefinition.Members.ContentDirectory;

        foreach (var attributeInfo in capture.AttributeMembers.Where(
                     info => info.Properties.ContainsKey(pageRoute)
                             && info.Properties.ContainsKey(contentDirectory)))
        {
            try
            {
                var routeName = attributeInfo.Properties[pageRoute];
                var routeDir  = Path.Combine(capture.Properties.ProjectDir, pagesDirName, routeName);
                var pageFile = Directory.GetFiles(routeDir, "*.razor", SearchOption.AllDirectories)
                                        .FirstOrDefault(
                                            file =>
                                            {
                                                var split             = file.Split(Path.DirectorySeparatorChar);
                                                var fileWithExtension = split[^1];
                                                return fileWithExtension.StartsWith("[")
                                                       && fileWithExtension.EndsWith("].razor");
                                            });

                if (string.IsNullOrWhiteSpace(pageFile))
                    continue;

                var collectionDir = Path.Combine(
                    capture.Properties.ProjectDir,
                    contentDirName,
                    attributeInfo.Properties[contentDirectory]);
                var collectionRootDir = collectionDir[..collectionDir.LastIndexOf(Path.DirectorySeparatorChar)];
                var markdownFiles = Directory.GetFiles(collectionDir, "*.md", SearchOption.AllDirectories)
                                             .Select(file => $"@\"{file}\"");

                var routeNameNoSpecialChars = new Regex("[^a-zA-Z0-9]").Replace(routeName, "");
                var className =
                    $"Implementations_{Constants.Interfaces.PageCollectionDefinition.Name.Replace("Page", routeNameNoSpecialChars)}";

                context.AddSource(
                    $"{className}.generated.cs",
                    $$"""
                      // <auto-generated/>
                      using Microsoft.AspNetCore.Components;
                      using Microsoft.AspNetCore.Components.Web;
                      using {{Constants.RazorStaticAbstractionsNamespace}};
                      using {{Constants.RazorStaticComponentsNamespace}};
                      using System;
                      using System.Collections.Frozen;
                      using System.Collections.Generic;
                      using System.Threading.Tasks;

                      namespace {{Constants.RazorStaticCoreNamespace}}
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
            "Implementations_PageCollectionsStore.generated.cs",
            $$"""
              // <auto-generated/>
              using Microsoft.AspNetCore.Components.Web;
              using {{Constants.RazorStaticAbstractionsNamespace}};
              using System.Collections.Frozen;
              using System.Collections.Generic;
              using System.Diagnostics.CodeAnalysis;

              namespace {{Constants.RazorStaticCoreNamespace}}
              {
                  internal sealed class Implementations_PageCollectionsStore : {{Constants.Interfaces.PageCollectionsStore.Name}}
                  {
              #nullable enable
                      private readonly HtmlRenderer _renderer;
                      private readonly FrozenDictionary<string, {{Constants.Interfaces.PageCollectionDefinition.Name}}> _collections;
                      
                      public Implementations_PageCollectionsStore(HtmlRenderer renderer)
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

    private static Func<AnalyzerConfigOptionsProvider, CancellationToken, CsProjProperties> ReadCsProj() =>
        static (provider, _) => DirectoryUtils.ReadCsProj(provider.GlobalOptions);

    private static string GetDirectoryToPageTypePair(string filePath, Capture capture) =>
        $"[@\"{filePath}\"] = {DirectoryUtils.GetPageType(filePath, capture.Properties.ProjectDir, capture.AssemblyName!)}";
}
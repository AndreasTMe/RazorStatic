using Microsoft.CodeAnalysis;
using RazorStatic.SourceGen.Extensions;
using RazorStatic.SourceGen.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Capture = RazorStatic.SourceGen.Utilities.Capture;

namespace RazorStatic.SourceGen.Pipelines;

internal static partial class GeneratorPipelines
{
    private const string Key         = Constants.Attributes.CollectionDefinition.Members.Key;
    private const string PageRoute   = Constants.Attributes.CollectionDefinition.Members.PageRoute;
    private const string ContentDir  = Constants.Attributes.CollectionDefinition.Members.ContentDirectory;
    private const string StorageType = Constants.Attributes.CollectionDefinition.Members.StorageType;

    private const string ExtKey       = Constants.Attributes.CollectionExtension.Members.Key;
    private const string ExtPageRoute = Constants.Attributes.CollectionExtension.Members.PageRoute;
    private const string ExtGroupBy   = Constants.Attributes.CollectionExtension.Members.GroupBy;

    private sealed record FileMetadata(string Name, string Frontmatter);

    public static void ExecutePageCollectionsPipeline(SourceProductionContext context, Capture capture)
    {
        if (string.IsNullOrWhiteSpace(capture.Properties.ProjectDir)
            || string.IsNullOrWhiteSpace(capture.AssemblyName)
            || capture.AttributeMembers.IsDefaultOrEmpty)
            return;

        var pagesForFactory               = new Dictionary<string, string>();
        var pageRoutesToFilesMap          = new Dictionary<string, List<string>>();
        var extensionsToPaths             = new Dictionary<string, string>();
        var extensionsToContentFileGroups = new Dictionary<string, Dictionary<string, HashSet<FileMetadata>>>();

        try
        {
            var pagesDirName = capture.DirectorySetup.Properties[Constants.Attributes.DirectoriesSetup.Members.Pages];
            var pagesDir     = Path.Combine(capture.Properties.ProjectDir, pagesDirName).EnsurePathSeparator();

            var contentDirName =
                capture.DirectorySetup.Properties[Constants.Attributes.DirectoriesSetup.Members.Content];
            var contentDir = Path.Combine(capture.Properties.ProjectDir, contentDirName).EnsurePathSeparator();

            foreach (var attributeInfo in capture.AttributeMembers
                         .Where(static info => info.Properties.ContainsKey(Key)
                                               && info.Properties.ContainsKey(PageRoute)
                                               && info.Properties.ContainsKey(ContentDir))
                         .Where(static attributeInfo => !string.IsNullOrWhiteSpace(attributeInfo.Properties[Key])
                                                        && !string.IsNullOrWhiteSpace(
                                                            attributeInfo.Properties[PageRoute])
                                                        && !string.IsNullOrWhiteSpace(
                                                            attributeInfo.Properties[ContentDir])))
            {
                // Handle storage type

                if (!attributeInfo.Properties.TryGetValue(StorageType, out var storageType)
                    || string.IsNullOrWhiteSpace(storageType))
                {
                    // Default to local storage
                    storageType = Constants.Enums.StorageType.Name + "." + Constants.Enums.StorageType.Values.Local;
                }

                if (!TryGetPageCollectionFilePath(pagesDir, attributeInfo.Properties[PageRoute], out var pageFilePath))
                {
                    continue;
                }

                var pageRouteName = GetRouteNameNoSpecialChars(attributeInfo.Properties[PageRoute]);
                var className =
                    $"Implementations_{Constants.Interfaces.PageCollectionDefinition.Name.Replace("Page", pageRouteName)}";

                string source;

                if (storageType.EndsWith(Constants.Enums.StorageType.Values.Local, StringComparison.Ordinal))
                {
                    // Handle content files

                    var pageCollectionDir =
                        Path.Combine(contentDir, attributeInfo.Properties[ContentDir]).EnsurePathSeparator();
                    var collectionContentFiles = GetContentFiles(pageCollectionDir);

                    pageRoutesToFilesMap[pageRouteName] = collectionContentFiles;

                    // Handle extensions

                    extensionsToPaths.Clear();
                    extensionsToContentFileGroups.Clear();

                    foreach (var (file, groups) in GetContentFileGroups(
                                 attributeInfo.Properties[Key],
                                 pagesDir,
                                 collectionContentFiles,
                                 capture.AttributeExtensionMembers.Where(static m => m.Properties.ContainsKey(ExtKey)
                                     && m.Properties.ContainsKey(ExtPageRoute)
                                     && m.Properties.ContainsKey(ExtGroupBy))))
                    {
                        var pageType = DirectoryUtils.GetPageType(
                            file,
                            capture.Properties.ProjectDir,
                            capture.AssemblyName);
                        if (!extensionsToPaths.ContainsKey(pageType))
                        {
                            extensionsToPaths[pageType] = file;
                        }

                        if (!extensionsToContentFileGroups.TryGetValue(pageType, out var existingFileGroup))
                        {
                            extensionsToContentFileGroups[pageType] = groups;
                        }
                        else
                        {
                            foreach (var group in groups)
                            {
                                if (!existingFileGroup.ContainsKey(group.Key))
                                {
                                    existingFileGroup[group.Key] = group.Value;
                                }
                                else
                                {
                                    existingFileGroup[group.Key].UnionWith(group.Value);
                                }
                            }
                        }
                    }

                    source = GenerateLocalPageCollectionDefinitionSource(
                        className,
                        contentDir,
                        collectionContentFiles,
                        extensionsToContentFileGroups);
                }
                else
                {
                    // Handle extensions

                    extensionsToPaths.Clear();
                    var extensionsToGroupBy = new Dictionary<string, string>();

                    // We've only got the razor files for remote content
                    foreach (var (file, groups) in GetContentFileGroups(
                                 attributeInfo.Properties[Key],
                                 pagesDir,
                                 [],
                                 capture.AttributeExtensionMembers.Where(static m =>
                                     m.Properties.ContainsKey(ExtKey)
                                     && m.Properties.ContainsKey(ExtPageRoute)
                                     && m.Properties.ContainsKey(ExtGroupBy))))
                    {
                        var pageType = DirectoryUtils.GetPageType(
                            file,
                            capture.Properties.ProjectDir,
                            capture.AssemblyName);
                        if (!extensionsToPaths.ContainsKey(pageType))
                        {
                            extensionsToPaths[pageType] = file;
                        }

                        // There's always going to be a single category per type
                        extensionsToGroupBy[pageType] = groups.First().Key;
                    }

                    if (storageType.EndsWith(Constants.Enums.StorageType.Values.AzureBlob, StringComparison.Ordinal))
                    {
                        source = GenerateAzureBlobPageCollectionDefinitionSource(className, extensionsToGroupBy);
                    }
                    else if (storageType.EndsWith(Constants.Enums.StorageType.Values.AwsS3, StringComparison.Ordinal))
                    {
                        // TODO: Add diagnostic, not supported for now
                        continue;
                    }
                    else
                    {
                        // TODO: Unsupported value? Error?
                        continue;
                    }
                }

                context.AddSource($"{className}.generated.cs", source);

                pagesForFactory[pageFilePath] = className;
                foreach (var kvp in extensionsToPaths)
                {
                    pagesForFactory[kvp.Value] = className;
                }
            }

            context.AddSource(
                $"Implementations_{Constants.Interfaces.PageCollectionsStore.Name}.generated.cs",
                $$"""
                  // <auto-generated/>
                  using Microsoft.AspNetCore.Components.Web;
                  using Microsoft.Extensions.DependencyInjection;
                  using {{Constants.RazorStaticAbstractionsNamespace}};
                  using {{Constants.RazorStaticUtilitiesNamespace}};
                  using System.Collections.Frozen;
                  using System.Collections.Generic;
                  using System.Diagnostics.CodeAnalysis;
                  using System.IO;
                  using System;
                  using System.Linq;

                  namespace {{Constants.RazorStaticCoreNamespace}}
                  {
                      internal sealed class Implementations_{{Constants.Interfaces.PageCollectionsStore.Name}} : {{Constants.Interfaces.PageCollectionsStore.Name}}
                      {
                  #nullable enable
                          private readonly FrozenDictionary<string, {{Constants.Interfaces.PageCollectionDefinition.Name}}> _collections;
                          
                          public Implementations_{{Constants.Interfaces.PageCollectionsStore.Name}}(IServiceScopeFactory scopeFactory)
                          {
                              _collections = new Dictionary<string, {{Constants.Interfaces.PageCollectionDefinition.Name}}>
                              {
                                  {{string.Join(",\n                ", pagesForFactory.Select(static kvp => $"[@\"{kvp.Key}\"] = new {kvp.Value}(scopeFactory)"))}}
                              }
                              .ToFrozenDictionary();
                          }
                          
                          public bool {{Constants.Interfaces.PageCollectionsStore.Members.TryGetCollection}}(string filePath, [MaybeNullWhen(false)] out {{Constants.Interfaces.PageCollectionDefinition.Name}} collection)
                          {
                              return _collections.TryGetValue(filePath, out collection);
                          }
                  #nullable disable
                      }
                  }
                  """);

            foreach (var kvp in pageRoutesToFilesMap)
            {
                var pageKey = string.Concat(kvp.Key[0].ToString().ToUpper(), kvp.Key.Substring(1));

                context.AddSource(
                    $"Helpers_{pageKey}Collection.generated.cs",
                    $$"""
                      // <auto-generated/>
                      using {{Constants.RazorStaticUtilitiesNamespace}};
                      using System;
                      using System.Collections.Frozen;
                      using System.Collections.Generic;
                      using System.IO;
                      using System.Linq;

                      namespace {{Constants.RazorStaticCoreNamespace}}
                      {
                          public static class {{pageKey}}Collection
                          {
                      #nullable enable
                              private static Lazy<FrozenDictionary<string, string>> _all = new Lazy<FrozenDictionary<string, string>>(() =>
                              {
                                  return new HashSet<string>()
                                  {
                                      {{string.Join(",\n                ", kvp.Value)}}
                                  }
                                  .Select(contentFile => (SlugUtils.Convert(Path.GetFileNameWithoutExtension(contentFile).ToLowerInvariant()), contentFile))
                                  .ToFrozenDictionary(kvp => kvp.Item1, kvp => kvp.Item2);
                              });
                              
                              public static FrozenDictionary<string, string> All => _all.Value;
                      #nullable disable
                          }
                      }
                      """);
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static string GenerateLocalPageCollectionDefinitionSource(
        string className,
        string contentDir,
        IEnumerable<string> collectionContentFiles,
        Dictionary<string, Dictionary<string, HashSet<FileMetadata>>> extensionsToContentFileGroups) =>
        $$"""
          // <auto-generated/>
          using Microsoft.AspNetCore.Components;
          using Microsoft.AspNetCore.Components.Web;
          using Microsoft.Extensions.DependencyInjection;
          using {{Constants.RazorStaticAbstractionsNamespace}};
          using {{Constants.RazorStaticComponentsNamespace}};
          using {{Constants.RazorStaticUtilitiesNamespace}};
          using System;
          using System.Collections.Frozen;
          using System.Collections.Generic;
          using System.Linq;
          using System.IO;
          using System.Runtime.CompilerServices;
          using System.Text;
          using System.Threading;
          using System.Threading.Tasks;

          namespace {{Constants.RazorStaticCoreNamespace}}
          {
              internal sealed class {{className}} : {{Constants.Abstractions.PageCollectionDefinition.Name}}
              {
          #nullable enable
                  private readonly IServiceScopeFactory _scopeFactory;
                  
                  public override string {{Constants.Abstractions.PageCollectionDefinition.Members.RootPath}} => @"{{contentDir}}";
                  
                  public {{className}}(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

                  public override async IAsyncEnumerable<RenderedResult> {{Constants.Abstractions.PageCollectionDefinition.Members.RenderComponentsAsync}}(Type pageType, [EnumeratorCancellation] CancellationToken cancellationToken)
                  {
                      await using var scope    = _scopeFactory.CreateAsyncScope();
                      await using var renderer = scope.ServiceProvider.GetRequiredService<HtmlRenderer>();
                  
                      foreach (var (slug, contentFilePath) in ContentFiles.SlugsToPaths)
                      {
                          if (cancellationToken.IsCancellationRequested) yield break;
                          
                          (string? Frontmatter, string? Markdown) fileContent;
                          using (var reader = File.OpenText(contentFilePath))
                          {
                              fileContent = await {{Constants.Abstractions.PageCollectionDefinition.Members.GetFileContentAsync}}(reader, cancellationToken);
                          }
                          
                          var content = await renderer.Dispatcher.InvokeAsync(async () =>
                          {
                              var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                              {
                                  [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.ContentFilePath}})] = contentFilePath,
                                  [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.Slug}})] = slug,
                                  [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.FrontMatter}})] = fileContent.Frontmatter,
                                  [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.Content}})] = fileContent.Markdown
                              });
                              var output = await renderer.RenderComponentAsync(pageType, parameters);
                              return output.ToHtmlString();
                          });
                          yield return new RenderedResult(contentFilePath, content);
                      }
                  }
                  
                  public override async IAsyncEnumerable<RenderedResult> {{Constants.Abstractions.PageCollectionDefinition.Members.RenderGroupComponentsAsync}}(Type pageType, [EnumeratorCancellation] CancellationToken cancellationToken)
                  {
                      if (!Extensions.MetadataGroups.TryGetValue(pageType, out var metadataPerGroup))
                      {
                          yield break;
                      }
                      
                      await using var scope    = _scopeFactory.CreateAsyncScope();
                      await using var renderer = scope.ServiceProvider.GetRequiredService<HtmlRenderer>();
                  
                      foreach (var (group, metadata) in metadataPerGroup)
                      {
                          if (cancellationToken.IsCancellationRequested) yield break;
                          
                          var slug = SlugUtils.Convert(group.ToLowerInvariant());
                          var content = await renderer.Dispatcher.InvokeAsync(async () =>
                          {
                              var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                              {
                                  [nameof({{Constants.Abstractions.CollectionFileGroupComponentBase.Name}}.{{Constants.Abstractions.CollectionFileGroupComponentBase.Members.Slug}})] = slug,
                                  [nameof({{Constants.Abstractions.CollectionFileGroupComponentBase.Name}}.{{Constants.Abstractions.CollectionFileGroupComponentBase.Members.GroupBy}})] = group,
                                  [nameof({{Constants.Abstractions.CollectionFileGroupComponentBase.Name}}.{{Constants.Abstractions.CollectionFileGroupComponentBase.Members.Metadata}})] = metadata
                              });
                              var output = await renderer.RenderComponentAsync(pageType, parameters);
                              return output.ToHtmlString();
                          });
                          yield return new RenderedResult(slug, content);
                      }
                  }
          #nullable disable
              }
              
              file static class ContentFiles
              {
                  public static readonly FrozenDictionary<string, string> SlugsToPaths = new HashSet<string>()
                  {
                      {{string.Join(",\n            ", collectionContentFiles)}}
                  }
                  .Select(contentFile => (SlugUtils.Convert(Path.GetFileNameWithoutExtension(contentFile).ToLowerInvariant()), contentFile))
                  .ToFrozenDictionary(kvp => kvp.Item1, kvp => kvp.Item2);
              }
              
              file static class Extensions
              {
                  public static readonly FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<ValueTuple<string, string>>>> MetadataGroups = new Dictionary<Type, FrozenDictionary<string, FrozenSet<ValueTuple<string, string>>>>()
                  {
                      {{string.Join(
                          ",\n            ",
                          extensionsToContentFileGroups.Select(
                              static x => $"[{x.Key}] = new Dictionary<string, FrozenSet<ValueTuple<string, string>>>\n            {{\n                {
                                  string.Join(
                                      ",\n                ",
                                      x.Value.Select(static y => $"[\"{y.Key}\"] = new HashSet<ValueTuple<string, string>>\n                {{\n                    {
                                          string.Join(",\n                    ", y.Value.Select(
                                              static z => $"(SlugUtils.Convert(\"{z.Name}\"), \"{
                                                  z.Frontmatter.Trim().Replace("\r\n", @"\r\n").Replace("\"", "\\\"")
                                              }\")"))
                                      }\n                }}.ToFrozenSet()"))
                              }\n            }}.ToFrozenDictionary()"))}}
                  }
                  .ToFrozenDictionary();
              }
          }
          """;

    private static string GenerateAzureBlobPageCollectionDefinitionSource(
        string className,
        Dictionary<string, string> extensionsToGroupBy) =>
        $$"""
          // <auto-generated/>
          using Azure.Storage.Blobs;
          using Microsoft.AspNetCore.Components;
          using Microsoft.AspNetCore.Components.Web;
          using Microsoft.Extensions.DependencyInjection;
          using {{Constants.RazorStaticAbstractionsNamespace}};
          using {{Constants.RazorStaticComponentsNamespace}};
          using {{Constants.RazorStaticUtilitiesNamespace}};
          using System;
          using System.Collections.Concurrent;
          using System.Collections.Frozen;
          using System.Collections.Generic;
          using System.IO;
          using System.Linq;
          using System.Runtime.CompilerServices;
          using System.Threading;
          using System.Threading.Tasks;

          namespace {{Constants.RazorStaticCoreNamespace}}
          {
              internal sealed class {{className}} : {{Constants.Abstractions.PageCollectionDefinition.Name}}
              {
          #nullable enable
                  private readonly IServiceScopeFactory _scopeFactory;

                  public override string {{Constants.Abstractions.PageCollectionDefinition.Members.RootPath}} => string.Empty;
                  
                  public {{className}}(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;
                  
                  public override async IAsyncEnumerable<RenderedResult> {{Constants.Abstractions.PageCollectionDefinition.Members.RenderComponentsAsync}}(Type pageType, [EnumeratorCancellation] CancellationToken cancellationToken)
                  {
                      await using var scope    = _scopeFactory.CreateAsyncScope();
                      await using var renderer = scope.ServiceProvider.GetRequiredService<HtmlRenderer>();
                      
                      var blobContainerClient = scope.ServiceProvider.GetRequiredService<BlobContainerClient>();
                      
                      await foreach (var blobItem in blobContainerClient.GetBlobsAsync(cancellationToken: cancellationToken))
                      {
                          var blobClient = blobContainerClient.GetBlobClient(blobItem.Name);
                          var slug       = SlugUtils.Convert(Path.GetFileNameWithoutExtension(blobItem.Name).ToLowerInvariant());
                          
                          (string? Frontmatter, string? Markdown) fileContent;
                          await using (var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken))
                          {
                              using (var reader = new StreamReader(stream))
                              {
                                  fileContent = await {{Constants.Abstractions.PageCollectionDefinition.Members.GetFileContentAsync}}(reader, cancellationToken);
                              }
                          }
                              
                          var content = await renderer.Dispatcher.InvokeAsync(async () =>
                          {
                              var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                              {
                                  [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.ContentFilePath}})] = blobItem.Name,
                                  [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.Slug}})] = slug,
                                  [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.FrontMatter}})] = fileContent.Frontmatter,
                                  [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.Content}})] = fileContent.Markdown
                              });
                              var output = await renderer.RenderComponentAsync(pageType, parameters);
                              return output.ToHtmlString();
                          });
                  
                          yield return new RenderedResult(blobItem.Name, content);
                      }
                  }
                  
                  public override async IAsyncEnumerable<RenderedResult> {{Constants.Abstractions.PageCollectionDefinition.Members.RenderGroupComponentsAsync}}(Type pageType, [EnumeratorCancellation] CancellationToken cancellationToken)
                  {
                      await using var scope    = _scopeFactory.CreateAsyncScope();
                      await using var renderer = scope.ServiceProvider.GetRequiredService<HtmlRenderer>();
                      
                      var blobContainerClient = scope.ServiceProvider.GetRequiredService<BlobContainerClient>();
                      
                      var metadataGroups = await Extensions.GetMetadataGroupsAsync(blobContainerClient, GetFileContentAsync, cancellationToken);
                      
                      if (!metadataGroups.TryGetValue(pageType, out var metadataPerGroup))
                      {
                          yield break;
                      }
                      
                      foreach (var (group, metadata) in metadataPerGroup)
                      {
                          if (cancellationToken.IsCancellationRequested) yield break;
                      
                          var slug = SlugUtils.Convert(group.ToLowerInvariant());
                          var content = await renderer.Dispatcher.InvokeAsync(async () =>
                          {
                              var parameters = ParameterView.FromDictionary(
                                  new Dictionary<string, object?>
                                  {
                                      [nameof(CollectionFileGroupComponentBase.Slug)]     = slug,
                                      [nameof(CollectionFileGroupComponentBase.GroupBy)]  = group,
                                      [nameof(CollectionFileGroupComponentBase.Metadata)] = metadata
                                  });
                              var output = await renderer.RenderComponentAsync(pageType, parameters);
                              return output.ToHtmlString();
                          });
                          yield return new RenderedResult(slug, content);
                      }
                  }
          #nullable disable
              }
              
              file static class Extensions
              {
          #nullable enable
                  private static readonly FrozenDictionary<Type, string> PageTypeToGroupKey = new Dictionary<Type, string>
                  {
                      {{string.Join(",\n            ", extensionsToGroupBy.Select(static kvp => $"[{kvp.Key}] = \"{kvp.Value}\""))}}
                  }
                  .ToFrozenDictionary();
              
                  private static FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<ValueTuple<string, string>>>>? _metadataGroups;
              
                  public static async Task<FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<(string Slug, string FrontMatter)>>>> GetMetadataGroupsAsync(
                      BlobContainerClient containerClient,
                      Func<TextReader, CancellationToken, Task<(string? FrontMatter, string? Markdown)>> getFileContentAction,
                      CancellationToken cancellationToken)
                  {
                      if (_metadataGroups is not null)
                      {
                          return _metadataGroups;
                      }
              
                      var metadataGroups = new ConcurrentDictionary<Type, ConcurrentDictionary<string, ConcurrentBag<ValueTuple<string, string>>>>();
              
                      await Parallel.ForEachAsync(
                          containerClient.GetBlobsAsync(cancellationToken: cancellationToken),
                          new ParallelOptions
                          {
                              CancellationToken      = cancellationToken,
                              MaxDegreeOfParallelism = 8
                          },
                          async (blobItem, token) =>
                          {
                              var blobClient = containerClient.GetBlobClient(blobItem.Name);
                              var slug       = SlugUtils.Convert(Path.GetFileNameWithoutExtension(blobItem.Name).ToLowerInvariant());
              
                              await using var stream = await blobClient.OpenReadAsync(cancellationToken: token);
                              using var       reader = new StreamReader(stream);
              
                              var (frontmatter, _) = await getFileContentAction.Invoke(reader, token);
                              if (string.IsNullOrWhiteSpace(frontmatter))
                              {
                                  return;
                              }
              
                              foreach (var (type, groupBy) in PageTypeToGroupKey)
                              {
                                  if (!frontmatter.Split("/r/n").Any(line => line.StartsWith(groupBy, StringComparison.Ordinal)))
                                  {
                                      continue;
                                  }
              
                                  if (!metadataGroups.TryGetValue(type, out var groupByToFrontmatters))
                                  {
                                      groupByToFrontmatters = [];
                                      metadataGroups[type]  = groupByToFrontmatters;
                                  }
              
                                  if (!groupByToFrontmatters.TryGetValue(groupBy, out var frontmatters))
                                  {
                                      frontmatters                  = [];
                                      metadataGroups[type][groupBy] = frontmatters;
                                  }
              
                                  metadataGroups[type][groupBy].Add((slug, frontmatter));
                              }
                          });
              
                      _metadataGroups = metadataGroups.ToFrozenDictionary(
                          static x => x.Key,
                          static x => x.Value.ToFrozenDictionary(
                              static y => y.Key,
                              static y => y.Value.ToFrozenSet()));
              
                      return _metadataGroups;
                  }
              }
          #nullable disable
          }
          """;

    private static bool TryGetPageCollectionFilePath(string pagesDir, string pageRoute, out string pageFile)
    {
        var routeDir = Path.Combine(pagesDir, pageRoute);
        pageFile = Directory.GetFiles(routeDir, "*.razor", SearchOption.AllDirectories)
                       .FirstOrDefault(static file =>
                       {
                           var split             = file.Split(Path.DirectorySeparatorChar);
                           var fileWithExtension = split[split.Length - 1];
                           return fileWithExtension.StartsWith("[", StringComparison.Ordinal)
                                  && fileWithExtension.EndsWith("].razor", StringComparison.Ordinal);
                       })
                   ?? string.Empty;

        return !string.IsNullOrWhiteSpace(pageFile);
    }

    private static List<string> GetContentFiles(string collectionDir) =>
        Directory.GetFiles(collectionDir, "*.md", SearchOption.AllDirectories)
            .Select(static file => $"@\"{file}\"")
            .ToList();

    private static string GetRouteNameNoSpecialChars(string pageRoute)
    {
        pageRoute = new Regex("[^a-zA-Z0-9]").Replace(pageRoute, "");
        return string.Concat(pageRoute[0].ToString().ToUpper(), pageRoute.Substring(1));
    }

    private static IEnumerable<(string file, Dictionary<string, HashSet<FileMetadata>> groups)> GetContentFileGroups(
        string collectionKey,
        string pagesDir,
        IEnumerable<string> collectionContentFiles,
        IEnumerable<AttributeMemberData> attributeExtensionMembers)
    {
        var frontmatterBuilder = new StringBuilder();
        var contentFiles       = collectionContentFiles.Select(static x => x.TrimStart('@').Trim('"')).ToList();

        foreach (var memberData in attributeExtensionMembers)
        {
            if (memberData.Properties[ExtKey] != collectionKey
                || string.IsNullOrWhiteSpace(memberData.Properties[ExtPageRoute])
                || string.IsNullOrWhiteSpace(memberData.Properties[ExtGroupBy]))
            {
                continue;
            }

            var extRoute    = memberData.Properties[ExtPageRoute].EnsurePathSeparator();
            var extRouteDir = Path.Combine(pagesDir, extRoute);
            var extPageFile = Directory.GetFiles(extRouteDir, "*.razor", SearchOption.AllDirectories)
                                  .FirstOrDefault(static file =>
                                  {
                                      var split             = file.Split(Path.DirectorySeparatorChar);
                                      var fileWithExtension = split[split.Length - 1];
                                      return fileWithExtension.StartsWith("[", StringComparison.Ordinal)
                                             && fileWithExtension.EndsWith("].razor", StringComparison.Ordinal);
                                  })
                              ?? string.Empty;

            if (string.IsNullOrWhiteSpace(extPageFile))
                continue;

            if (contentFiles.Count == 0)
            {
                yield return (extPageFile, new Dictionary<string, HashSet<FileMetadata>>
                {
                    [memberData.Properties[ExtGroupBy]] = []
                });
            }

            foreach (var contentFile in contentFiles)
            {
                if (!TryGetFrontmatter(
                        frontmatterBuilder,
                        contentFile,
                        memberData.Properties[ExtGroupBy],
                        out var frontmatter,
                        out var groupByEntries))
                {
                    continue;
                }

                var contentFileGroups = new Dictionary<string, HashSet<FileMetadata>>();

                var lastIndexOfDirSeparator = contentFile.LastIndexOf(Path.DirectorySeparatorChar) + 1;
                if (lastIndexOfDirSeparator < 0 || lastIndexOfDirSeparator >= contentFile.Length)
                {
                    lastIndexOfDirSeparator = 0;
                }

                var contentFileName = contentFile.Substring(lastIndexOfDirSeparator).Split('.')[0].ToLowerInvariant();

                foreach (var entry in groupByEntries)
                {
                    if (!contentFileGroups.ContainsKey(entry))
                    {
                        contentFileGroups[entry] = [];
                    }

                    contentFileGroups[entry].Add(new FileMetadata(contentFileName, frontmatter));
                }

                yield return (extPageFile, contentFileGroups);
            }
        }
    }

    private static bool TryGetFrontmatter(
        in StringBuilder frontmatterBuilder,
        in string contentFile,
        in string groupBy,
        out string frontmatter,
        out HashSet<string> groupByEntries)
    {
        frontmatter    = string.Empty;
        groupByEntries = [];

        using var fileStream   = File.OpenRead(contentFile);
        using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128);

        var line = streamReader.ReadLine() ?? string.Empty;
        if (!line.Equals("---", StringComparison.Ordinal))
        {
            return false;
        }

        var found    = false;
        var captured = false;

        while (!streamReader.EndOfStream)
        {
            line = streamReader.ReadLine();
            if (line is null || line.Equals("---", StringComparison.Ordinal))
            {
                break;
            }

            frontmatterBuilder.Append(line).AppendLine();

            if (!found)
            {
                found = line.StartsWith(groupBy, StringComparison.Ordinal);
            }

            if (found && !captured)
            {
                captured = true;

                var split = line.Split(':').Where(static l => !string.IsNullOrWhiteSpace(l)).ToArray();
                if (split.Length == 2)
                {
                    groupByEntries.UnionWith(split[1].Split(',').Select(static g => g.Trim()));
                }
                else
                {
                    while ((line = streamReader.ReadLine()) != null && !line.Equals("---", StringComparison.Ordinal))
                    {
                        frontmatterBuilder.Append(line).AppendLine();
                        if (!line.StartsWith("  - ", StringComparison.Ordinal))
                        {
                            break;
                        }

                        groupByEntries.Add(line.Substring(4).Trim());
                    }
                }
            }

            if (line is null || line.Equals("---", StringComparison.Ordinal))
            {
                break;
            }
        }

        frontmatter = frontmatterBuilder.ToString();
        frontmatterBuilder.Clear();

        return found;
    }
}
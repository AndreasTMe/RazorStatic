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
    private const string Key        = Constants.Attributes.CollectionDefinition.Members.Key;
    private const string PageRoute  = Constants.Attributes.CollectionDefinition.Members.PageRoute;
    private const string ContentDir = Constants.Attributes.CollectionDefinition.Members.ContentDirectory;

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
        var keysForDirectory              = new Dictionary<string, string>();
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
                // Handle content files

                if (!TryGetPageCollectionFilePath(pagesDir, attributeInfo.Properties[PageRoute], out var pageFilePath))
                {
                    continue;
                }

                var pageCollectionDir =
                    Path.Combine(contentDir, attributeInfo.Properties[ContentDir]).EnsurePathSeparator();
                var collectionContentFiles = GetContentFiles(pageCollectionDir);

                var pageRouteName = GetRouteNameNoSpecialChars(attributeInfo.Properties[PageRoute]);
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

                var className =
                    $"Implementations_{Constants.Interfaces.PageCollectionDefinition.Name.Replace("Page", pageRouteName)}";

                context.AddSource(
                    $"{className}.generated.cs",
                    $$"""
                      // <auto-generated/>
                      using Microsoft.AspNetCore.Components;
                      using Microsoft.AspNetCore.Components.Web;
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
                          internal sealed class {{className}} : {{Constants.Interfaces.PageCollectionDefinition.Name}}
                          {
                      #nullable enable
                              private readonly HtmlRenderer _renderer;
                              
                              public string {{Constants.Interfaces.PageCollectionDefinition.Members.RootPath}} => @"{{contentDir}}";
                              
                              public {{className}}(HtmlRenderer renderer) => _renderer = renderer;

                              public async IAsyncEnumerable<RenderedResult> {{Constants.Interfaces.PageCollectionDefinition.Members.RenderComponentsAsync}}(Type pageType, [EnumeratorCancellation] CancellationToken cancellationToken)
                              {
                                  foreach (var (slug, contentFilePath) in ContentFiles.SlugsToPaths)
                                  {
                                      if (cancellationToken.IsCancellationRequested) yield break;
                                      
                                      var content = await _renderer.Dispatcher.InvokeAsync(async () =>
                                      {
                                          var (frontmatter, markdown) = await GetFileContentAsync(contentFilePath);
                                          var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                                          {
                                              [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.ContentFilePath}})] = contentFilePath,
                                              [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.Slug}})] = slug,
                                              [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.FrontMatter}})] = frontmatter,
                                              [nameof({{Constants.Abstractions.CollectionFileComponentBase.Name}}.{{Constants.Abstractions.CollectionFileComponentBase.Members.Content}})] = markdown
                                          });
                                          var output = await _renderer.RenderComponentAsync(pageType, parameters);
                                          return output.ToHtmlString();
                                      });
                                      yield return new RenderedResult(contentFilePath, content);
                                  }
                              }
                              
                              public async IAsyncEnumerable<RenderedResult> {{Constants.Interfaces.PageCollectionDefinition.Members.RenderGroupComponentsAsync}}(Type pageType, [EnumeratorCancellation] CancellationToken cancellationToken)
                              {
                                  if (!Extensions.MetadataGroups.TryGetValue(pageType, out var metadataPerGroup))
                                  {
                                      yield break;
                                  }
                              
                                  foreach (var (group, metadata) in metadataPerGroup)
                                  {
                                      if (cancellationToken.IsCancellationRequested) yield break;

                                      var slug = SlugUtils.Convert(group.ToLowerInvariant());
                                      var content = await _renderer.Dispatcher.InvokeAsync(async () =>
                                      {
                                          var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                                          {
                                              [nameof({{Constants.Abstractions.CollectionFileGroupComponentBase.Name}}.{{Constants.Abstractions.CollectionFileGroupComponentBase.Members.Slug}})] = slug,
                                              [nameof({{Constants.Abstractions.CollectionFileGroupComponentBase.Name}}.{{Constants.Abstractions.CollectionFileGroupComponentBase.Members.GroupBy}})] = group,
                                              [nameof({{Constants.Abstractions.CollectionFileGroupComponentBase.Name}}.{{Constants.Abstractions.CollectionFileGroupComponentBase.Members.Metadata}})] = metadata
                                          });
                                          var output = await _renderer.RenderComponentAsync(pageType, parameters);
                                          return output.ToHtmlString();
                                      });
                                      yield return new RenderedResult(slug, content);
                                  }
                              }

                              private static async Task<(string? FrontMatter, string? Markdown)> GetFileContentAsync(string contentFilePath)
                              {
                                  using var streamReader = File.OpenText(contentFilePath);

                                  var line = await streamReader.ReadLineAsync();
                                  if (line is not "---")
                                  {
                                      var rest = await streamReader.ReadToEndAsync();
                                      return (string.Empty, (line + Environment.NewLine + rest).Trim());
                                  }
                                  
                                  var frontMatterBuilder = new StringBuilder();
                                  while (!streamReader.EndOfStream)
                                  {
                                      line = await streamReader.ReadLineAsync();
                                      if (line is "---")
                                      {
                                          break;
                                      }
                                      frontMatterBuilder.AppendLine(line);
                                  }
                                  
                                  var markdown = await streamReader.ReadToEndAsync();
                                  
                                  return (frontMatterBuilder.ToString().Trim(), markdown.Trim());
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
                      """);

                pagesForFactory[pageFilePath] = className;
                foreach (var kvp in extensionsToPaths)
                {
                    pagesForFactory[kvp.Value] = className;
                }
                keysForDirectory[attributeInfo.Properties[Key]] = pageCollectionDir;
            }

            context.AddSource(
                $"Implementations_{Constants.Interfaces.PageCollectionsStore.Name}.generated.cs",
                $$"""
                  // <auto-generated/>
                  using Microsoft.AspNetCore.Components.Web;
                  using {{Constants.RazorStaticAbstractionsNamespace}};
                  using System.Collections.Frozen;
                  using System.Collections.Generic;
                  using System.Diagnostics.CodeAnalysis;
                  using System.IO;

                  using {{Constants.RazorStaticUtilitiesNamespace}};
                  using System;
                  using System.Linq;

                  namespace {{Constants.RazorStaticCoreNamespace}}
                  {
                      internal sealed class Implementations_{{Constants.Interfaces.PageCollectionsStore.Name}} : {{Constants.Interfaces.PageCollectionsStore.Name}}
                      {
                  #nullable enable
                          private readonly HtmlRenderer _renderer;
                          private readonly FrozenDictionary<string, {{Constants.Interfaces.PageCollectionDefinition.Name}}> _collections;
                          private readonly FrozenDictionary<string, string> _directories;
                          
                          public Implementations_{{Constants.Interfaces.PageCollectionsStore.Name}}(HtmlRenderer renderer)
                          {
                              _renderer    = renderer;
                              _collections = new Dictionary<string, {{Constants.Interfaces.PageCollectionDefinition.Name}}>
                              {
                                  {{string.Join(",\n                ", pagesForFactory.Select(static kvp => $"[@\"{kvp.Key}\"] = new {kvp.Value}(renderer)"))}}
                              }
                              .ToFrozenDictionary();
                              _directories = new Dictionary<string, string>
                              {
                                  {{string.Join(",\n            ", keysForDirectory.Select(static kvp => $"[@\"{kvp.Key}\"] = @\"{kvp.Value}\""))}}
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
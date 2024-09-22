using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using RazorStatic.Shared;
using RazorStatic.Shared.Attributes;
using RazorStatic.Shared.Components;
using RazorStatic.SourceGen.Extensions;
using RazorStatic.SourceGen.Utilities;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

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

        var syntaxProvider = context.SyntaxProvider
                                    .CreateSyntaxProvider(
                                        static (node, _) => node.IsTargetAttributeNode(CollectionDefinition),
                                        static (ctx, _) => GetTargetAttributeNodeData(ctx.Node, ctx.SemanticModel))
                                    .Where(
                                        static classInfo =>
                                            !string.IsNullOrWhiteSpace(classInfo.ClassName)
                                            && !string.IsNullOrWhiteSpace(classInfo.Namespace));

        var compilationProvider = context.CompilationProvider
                                         .Select(static (compilation, _) => compilation.AssemblyName)
                                         .Combine(configOptionsProvider)
                                         .Select(static (combine, _) => new Capture(combine.Right, combine.Left))
                                         .Combine(syntaxProvider.Collect())
                                         .Select(
                                             static (combine, _) => combine.Left with
                                             {
                                                 ClassInfos = combine.Right
                                             });

        context.RegisterSourceOutput(compilationProvider, Execute);
    }

    private static AttributeClassInfo GetTargetAttributeNodeData(SyntaxNode node, SemanticModel semanticModel)
    {
        var attributeSyntax = (AttributeSyntax)node;
        var properties      = new Dictionary<string, string>();

        foreach (var argument in attributeSyntax.ArgumentList!.Arguments.Where(syntax => syntax.NameEquals is not null))
        {
            if (semanticModel.GetOperation(argument) is not ISimpleAssignmentOperation operation)
                continue;

            if (operation.Value.ConstantValue is { HasValue: true, Value: string value })
                properties[argument.NameEquals!.Name.ToString()] = value;
        }

        var classDeclarationSyntax = attributeSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();

        return new AttributeClassInfo(
            classDeclarationSyntax?.Identifier.ToString() ?? string.Empty,
            classDeclarationSyntax.GetFullNamespace(),
            classDeclarationSyntax?.Modifiers.Select(m => m.Text).ToImmutableArray() ?? ImmutableArray<string>.Empty,
            properties.ToFrozenDictionary());
    }

    private static void Execute(SourceProductionContext context, Capture capture)
    {
        if (string.IsNullOrWhiteSpace(capture.Properties.ProjectDir)
            || string.IsNullOrWhiteSpace(capture.Properties.PagesDir)
            || string.IsNullOrWhiteSpace(capture.Properties.ContentDir)
            || string.IsNullOrWhiteSpace(capture.AssemblyName)
            || capture.ClassInfos.IsDefaultOrEmpty)
            return;

        var pagesForFactory = new Dictionary<string, string>();

        foreach (var classInfo in capture.ClassInfos.Where(
                     info => info.Properties.ContainsKey(PageRoute) && info.Properties.ContainsKey(ContentDirectory)))
        {
            try
            {
                var routeDir = capture.Properties.PagesDir + classInfo.Properties[PageRoute];
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

                var collectionDir = capture.Properties.ContentDir + classInfo.Properties[ContentDirectory];
                var markdownFiles = Directory.GetFiles(collectionDir, "*.md", SearchOption.AllDirectories);

                context.AddSource(
                    $"RazorStatic_{classInfo.ClassName}.g.cs",
                    $$"""
                      using Microsoft.AspNetCore.Components;
                      using Microsoft.AspNetCore.Components.Web;
                      using RazorStatic.Shared;
                      using RazorStatic.Shared.Components;
                      using System;
                      using System.Collections.Frozen;
                      using System.Collections.Generic;
                      using System.Threading.Tasks;

                      namespace {{classInfo.Namespace}}
                      {
                          {{string.Join(" ", classInfo.Modifiers)}} class {{classInfo.ClassName}} : {{nameof(ICollectionPagesStore)}}
                          {
                      #nullable enable
                              private static readonly FrozenSet<string> Items = new HashSet<string>()
                              {
                                  {{string.Join(",\n            ", markdownFiles.Select(file => $"@\"{file}\""))}}
                              }
                              .ToFrozenSet();
                              
                              private readonly HtmlRenderer _renderer;
                              
                              public string {{nameof(ICollectionPagesStore.RootPath)}} => @"{{capture.Properties.ContentDir}}";
                              
                              public {{classInfo.ClassName}}(HtmlRenderer renderer) => _renderer = renderer;
                      
                              public async IAsyncEnumerable<RenderedResult> {{nameof(ICollectionPagesStore.RenderComponentsAsync)}}(Type pageType)
                              {
                                  foreach (var item in Items)
                                  {
                                      var content = await _renderer.Dispatcher.InvokeAsync(async () =>
                                      {
                                          var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof({{nameof(FileComponentBase)}}.{{nameof(FileComponentBase.FilePath)}})] = item });
                                          var output = await _renderer.RenderComponentAsync(pageType, parameters).ConfigureAwait(false);
                                          return output.ToHtmlString();
                                      });
                                      yield return new RenderedResult(item, content);
                                  }
                              }
                      #nullable disable
                          }
                      }
                      """);

                pagesForFactory[pageFile] = classInfo.ClassName;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        context.AddSource(
            "RazorStatic_CollectionPagesStoreFactory.g.cs",
            $$"""
              using Microsoft.AspNetCore.Components.Web;
              using RazorStatic.Shared;
              using System.Collections.Frozen;
              using System.Collections.Generic;
              using System.Diagnostics.CodeAnalysis;

              namespace {{capture.AssemblyName}}
              {
                  internal sealed class RazorStatic_CollectionPagesStoreFactory : {{nameof(ICollectionPagesStoreFactory)}}
                  {
              #nullable enable
                      private readonly HtmlRenderer _renderer;
                      private readonly FrozenDictionary<string, {{nameof(ICollectionPagesStore)}}> _collections;
                      
                      public RazorStatic_CollectionPagesStoreFactory(HtmlRenderer renderer)
                      {
                          _renderer    = renderer;
                          _collections = new Dictionary<string, {{nameof(ICollectionPagesStore)}}>
                          {
                              {{string.Join(",\n            ", pagesForFactory.Select(kvp => $"[@\"{kvp.Key}\"] = new {kvp.Value}(renderer)"))}}
                          }
                          .ToFrozenDictionary();
                      }
                      
                      public bool {{nameof(ICollectionPagesStoreFactory.TryGetCollection)}}(string key, [MaybeNullWhen(false)] out {{nameof(ICollectionPagesStore)}} collection)
                      {
                          return _collections.TryGetValue(key, out collection);
                      }
              #nullable disable
                  }
              }
              """);
    }
}
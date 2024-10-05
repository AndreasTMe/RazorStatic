using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RazorStatic.Shared;
using RazorStatic.Shared.Attributes;
using RazorStatic.Shared.Components;
using RazorStatic.SourceGen.Extensions;
using RazorStatic.SourceGen.Utilities;
using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace RazorStatic.SourceGen;

[Generator]
internal sealed class PagesStoreGenerator : IIncrementalGenerator
{
    private static readonly string PagesStore = nameof(PagesStoreAttribute)
        .Replace(nameof(Attribute), string.Empty);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configOptionsProvider = context.AnalyzerConfigOptionsProvider
                                           .Select(
                                               static (provider, _) =>
                                                   DirectoryUtils.ReadCsProj(provider.GlobalOptions));

        var syntaxProvider = context.SyntaxProvider
                                    .CreateSyntaxProvider(
                                        static (node, _) => node.IsTargetAttributeNode(PagesStore),
                                        static (ctx, _) => GetTargetAttributeNodeData(ctx.Node))
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

    private static AttributeClassInfo GetTargetAttributeNodeData(SyntaxNode node)
    {
        var attributeSyntax        = (AttributeSyntax)node;
        var classDeclarationSyntax = attributeSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();

        return new AttributeClassInfo(
            classDeclarationSyntax?.Identifier.ToString() ?? string.Empty,
            classDeclarationSyntax.GetFullNamespace(),
            classDeclarationSyntax?.Modifiers.Select(m => m.Text).ToImmutableArray() ?? ImmutableArray<string>.Empty,
            FrozenDictionary<string, string>.Empty);
    }

    private static void Execute(SourceProductionContext context, Capture capture)
    {
        if (string.IsNullOrWhiteSpace(capture.Properties.ProjectDir)
            || string.IsNullOrWhiteSpace(capture.Properties.PagesDir)
            || string.IsNullOrWhiteSpace(capture.AssemblyName)
            || capture.ClassInfos.Length != 1)
            return;

        try
        {
            var pages = Directory.GetFiles(capture.Properties.PagesDir, "*.razor", SearchOption.AllDirectories)
                                 .ToArray();

            var typeMappings = pages.Select(pagePath => GetDirectoryToPageTypePair(pagePath, capture));

            var classInfo = capture.ClassInfos[0];
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
                      {{string.Join(" ", classInfo.Modifiers)}} class {{classInfo.ClassName}} : {{nameof(IPagesStore)}}
                      {
                  #nullable enable
                          private static readonly FrozenDictionary<string, Type> Types = new Dictionary<string, Type>()
                          {
                              {{string.Join(",\n            ", typeMappings)}}
                          }
                          .ToFrozenDictionary();
                          
                          private readonly HtmlRenderer _renderer;
                  
                          public string {{nameof(IPagesStore.RootPath)}} => @"{{capture.Properties.PagesDir}}";
                          
                          public {{classInfo.ClassName}}(HtmlRenderer renderer) => _renderer = renderer;
                          
                          public Type GetPageType(string filePath) => Types[filePath];
                  
                          public Task<string> {{nameof(IPagesStore.RenderComponentAsync)}}(string filePath) => _renderer.Dispatcher.InvokeAsync(async () =>
                          {
                              var type = Types[filePath];
                              var parameters = type.IsSubclassOf(typeof({{nameof(FileComponentBase)}}))
                                  ? ParameterView.FromDictionary(new Dictionary<string, object?>
                                                                {
                                                                    [nameof({{nameof(FileComponentBase)}}.{{nameof(FileComponentBase.PageFilePath)}})] = filePath
                                                                })
                                  : ParameterView.Empty;
                  
                              var output = await _renderer.RenderComponentAsync(type, parameters).ConfigureAwait(false);
                              return output.ToHtmlString();
                          });
                          
                          public Task<string> {{nameof(IPagesStore.RenderLayoutComponentAsync)}}(string filePath, string htmlBody) => _renderer.Dispatcher.InvokeAsync(async () =>
                          {
                              var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                              {
                                  [nameof(LayoutComponentBase.Body)] = GetRenderFragment(htmlBody)
                              });
                              var output = await _renderer.RenderComponentAsync(Types[filePath], parameters).ConfigureAwait(false);
                              return output.ToHtmlString();
                          });
                          
                          private static RenderFragment GetRenderFragment(string html) => b => b.AddMarkupContent(0, html);
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
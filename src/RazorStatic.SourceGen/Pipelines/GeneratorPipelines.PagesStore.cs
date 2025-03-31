﻿using Microsoft.CodeAnalysis;
using RazorStatic.SourceGen.Utilities;
using System;
using System.IO;
using System.Linq;
using Capture = RazorStatic.SourceGen.Utilities.Capture;

namespace RazorStatic.SourceGen.Pipelines;

internal static partial class GeneratorPipelines
{
    public static void ExecutePagesStorePipeline(SourceProductionContext context, Capture capture)
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

            var typeMappings = pages.Select(pagePath => DirectoryUtils.GetDirectoryToPageTypePair(pagePath, capture));

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
                  #nullable disable
                          private static readonly FrozenDictionary<string, Type> Types = new Dictionary<string, Type>()
                          {
                              {{string.Join(",\n            ", typeMappings)}}
                          }
                          .ToFrozenDictionary();
                          
                          private readonly HtmlRenderer _renderer;
                  
                          public {{className}}(HtmlRenderer renderer) => _renderer = renderer;
                          
                          public Type {{Constants.Interfaces.PagesStore.Members.GetPageType}}(string filePath) => Types[filePath];
                  
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
                  #nullable enable
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
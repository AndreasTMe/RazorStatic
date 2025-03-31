﻿using Microsoft.CodeAnalysis;
using RazorStatic.SourceGen.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace RazorStatic.SourceGen.Pipelines;

internal static partial class GeneratorPipelines
{
    public static void
        ExecuteDirectoriesSetupForStaticContentPipeline(
            SourceProductionContext context,
            (CsProjProperties, ImmutableArray<AttributeMembers>) source)
    {
        var (csProj, attributes) = source;

        if (string.IsNullOrWhiteSpace(csProj.ProjectDir)
            || attributes.IsDefaultOrEmpty
            || attributes[0].MemberData.IsDefaultOrEmpty)
            return;

        var captures = new List<string>();
        foreach (var attribute in attributes[0].MemberData)
        {
            if (!attribute.Properties.TryGetValue(
                    Constants.Attributes.StaticContent.Members.RootPath,
                    out var rootPath)
                || rootPath == "null")
                continue;

            attribute.Properties.TryGetValue(
                Constants.Attributes.StaticContent.Members.Extensions,
                out var extensions);
            attribute.Properties.TryGetValue(
                Constants.Attributes.StaticContent.Members.EntryFile,
                out var entryFile);

            captures.Add(
                $"new(@\"{rootPath.Replace('/', Path.DirectorySeparatorChar)}\", {extensions ?? "[]"}, @\"{entryFile ?? ""}\")");
        }

        const string className = $"Implementations_{Constants.Interfaces.DirectoriesSetupForStaticContent.Name}";

        context.AddSource(
            $"{className}.generated.cs",
            $$"""
              // <auto-generated/>
              using {{Constants.RazorStaticAbstractionsNamespace}};
              using System;
              using System.Collections;
              using System.Collections.Frozen;
              using System.Collections.Generic;

              namespace {{Constants.RazorStaticGeneratedNamespace}}
              {
                  internal sealed class {{className}} : {{Constants.Interfaces.DirectoriesSetupForStaticContent.Name}}
                  {
              #nullable enable
                      private readonly FrozenSet<System.ValueTuple<string, string[], string>> _captures = new HashSet<ValueTuple<string, string[], string>>
                      {
                          {{string.Join(",\n            ", captures)}}
                      }.ToFrozenSet();
                  
                      public IEnumerator<ValueTuple<string, string[], string>> GetEnumerator() => _captures.GetEnumerator();
                      
                      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
              #nullable disable
                  }
              }
              """);
    }
}
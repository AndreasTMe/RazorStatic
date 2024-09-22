using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using RazorStatic.Shared;
using RazorStatic.Shared.Attributes;
using RazorStatic.SourceGen.Extensions;
using RazorStatic.SourceGen.Utilities;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace RazorStatic.SourceGen;

[Generator]
internal class TailwindConfigGenerator : IIncrementalGenerator
{
    private const string StylesFilePath = nameof(TailwindConfigAttribute.StylesFilePath);
    private const string OutputFilePath  = nameof(TailwindConfigAttribute.OutputFilePath);

    private static readonly string TailwindConfig = nameof(TailwindConfigAttribute)
        .Replace(nameof(Attribute), string.Empty);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider
                                    .CreateSyntaxProvider(
                                        static (node, _) => node.IsTargetAttributeNode(TailwindConfig),
                                        static (ctx, _) => GetTargetAttributeNodeData(ctx.Node, ctx.SemanticModel))
                                    .Where(
                                        static classInfo =>
                                            !string.IsNullOrWhiteSpace(classInfo.ClassName)
                                            && !string.IsNullOrWhiteSpace(classInfo.Namespace));

        var configOptionsProvider = context.AnalyzerConfigOptionsProvider
                                           .Select(
                                               static (provider, _) => new Capture(
                                                   DirectoryUtils.ReadCsProj(provider.GlobalOptions)))
                                           .Combine(syntaxProvider.Collect())
                                           .Select(
                                               static (combine, _) => combine.Left with
                                               {
                                                   ClassInfos = combine.Right
                                               });

        context.RegisterSourceOutput(configOptionsProvider, Execute);
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
            || string.IsNullOrWhiteSpace(capture.Properties.OutputPath)
            || string.IsNullOrWhiteSpace(capture.Properties.StylesDir)
            || capture.ClassInfos.Length != 1
            || !capture.ClassInfos[0].Properties.TryGetValue(StylesFilePath, out var stylesFilePath)
            || !capture.ClassInfos[0].Properties.TryGetValue(OutputFilePath, out var outputFilePath))
            return;

        var classInfo = capture.ClassInfos[0];

        string processStartInfoFileName;
        string processStartInfoArguments;
        var command = new StringBuilder()
                      .Append("npx tailwindcss")
                      .Append(" -i ")
                      .Append(Path.Combine(capture.Properties.StylesDir, stylesFilePath.TrimStart(Path.DirectorySeparatorChar)))
                      .Append(" -o ")
                      .Append(Path.Combine(capture.Properties.OutputPath, outputFilePath.TrimStart(Path.DirectorySeparatorChar)))
#if RELEASE
                      .Append(" --minify")
#endif
                      .ToString();

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            processStartInfoFileName  = "cmd.exe";
            processStartInfoArguments = $"/c {command}";
        }
        else
        {
            processStartInfoFileName  = "/bin/bash"; // or "/bin/sh" for broader compatibility
            processStartInfoArguments = $"-c \"{command}\"";
        }

        context.AddSource(
            $"RazorStatic_{classInfo.ClassName}.g.cs",
            $$"""
              using Microsoft.Extensions.Logging;
              using RazorStatic.Shared;
              using System;
              using System.Diagnostics;
              using System.Linq;
              using System.Text.RegularExpressions;
              using System.Threading.Tasks;

              namespace {{classInfo.Namespace}}
              {
                  {{string.Join(" ", classInfo.Modifiers)}} class {{classInfo.ClassName}} : {{nameof(ITailwindBuilder)}}
                  {
              #nullable enable
                      private static readonly Regex _splitProcessOutputRegex = new Regex("\\s{2,}");
                      
                      private readonly ILogger<{{classInfo.ClassName}}> _logger;
                      
                      public {{classInfo.ClassName}}(ILogger<{{classInfo.ClassName}}> logger)
                      {
                          _logger = logger;
                      }
                      
                      public Task {{nameof(ITailwindBuilder.BuildAsync)}}()
                      {
                          var process = Process.Start(new ProcessStartInfo
                          {
                              FileName               = "{{processStartInfoFileName}}",
                              Arguments              = @"{{processStartInfoArguments}}",
                              RedirectStandardOutput = true,
                              RedirectStandardError  = true,
                              UseShellExecute        = false,
                              CreateNoWindow         = true,
                              WorkingDirectory       = @"{{capture.Properties.ProjectDir}}"
                          });
                          ArgumentNullException.ThrowIfNull(process);
                      
                          _logger.LogInformation("{TwCommand}", @"{{command}}");
                          LogProcessOutput(process.StandardOutput.ReadToEnd());
                          LogProcessOutput(process.StandardError.ReadToEnd());
                      
                          return process.WaitForExitAsync();
                      }
                      
                      private void LogProcessOutput(string output)
                      {
                          if (string.IsNullOrWhiteSpace(output))
                              return;
                      
                          var parts = _splitProcessOutputRegex.Split(output);
                          foreach (var part in parts.Where(p => !string.IsNullOrWhiteSpace(p)))
                              _logger.LogInformation("{StdOut}", part.Trim());
                      }
              #nullable disable
                  }
              }
              """);
    }
}
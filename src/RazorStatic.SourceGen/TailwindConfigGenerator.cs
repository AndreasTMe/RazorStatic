using Microsoft.CodeAnalysis;
using RazorStatic.Shared;
using RazorStatic.Shared.Attributes;
using RazorStatic.Shared.Utilities;
using RazorStatic.SourceGen.Extensions;
using RazorStatic.SourceGen.Utilities;
using System;
using System.IO;
using System.Text;

namespace RazorStatic.SourceGen;

[Generator]
internal class TailwindConfigGenerator : IIncrementalGenerator
{
    private const string EntryFile = nameof(TailwindConfigAttribute.EntryFile);

    private static readonly string TailwindConfig = nameof(TailwindConfigAttribute)
        .Replace(nameof(Attribute), string.Empty);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var directoriesSetupSyntaxProvider = context.GetDirectoriesSetupSyntaxProvider();
        var syntaxProvider = context.SyntaxProvider
                                    .CreateSyntaxProvider(
                                        static (node, _) => node.IsTargetAttributeNode(TailwindConfig),
                                        static (ctx, _) => ctx.Node.GetAttributeMembers(ctx.SemanticModel));

        var configOptionsProvider = context.AnalyzerConfigOptionsProvider
                                           .Select(
                                               static (provider, _) => new Capture(
                                                   DirectoryUtils.ReadCsProj(provider.GlobalOptions)))
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

        context.RegisterSourceOutput(configOptionsProvider, Execute);
    }

    private static void Execute(SourceProductionContext context, Capture capture)
    {
        if (string.IsNullOrWhiteSpace(capture.Properties.ProjectDir)
            || string.IsNullOrWhiteSpace(capture.Properties.OutputPath)
            || !capture.DirectorySetup.Properties.TryGetValue(
                nameof(DirectoriesSetupAttribute.Tailwind),
                out var tailwindDir)
            || !capture.AttributeMembers[0].Properties.TryGetValue(EntryFile, out var stylesFilePath))
            return;

        string processStartInfoFileName;
        string processStartInfoArguments;

        var tailwindGeneratedOutputDir = Path.Combine(capture.Properties.OutputPath, Constants.Tailwind.Output);
        Directory.CreateDirectory(tailwindGeneratedOutputDir);

        var command = new StringBuilder()
                      .Append("npx tailwindcss")
                      .Append(" -i ")
                      .Append(
                          Path.Combine(
                              capture.Properties.ProjectDir,
                              tailwindDir,
                              stylesFilePath.TrimStart(Path.DirectorySeparatorChar)))
                      .Append(" -o ")
                      .Append(
                          Path.Combine(
                              tailwindGeneratedOutputDir,
                              stylesFilePath.Split(Path.DirectorySeparatorChar)[^1]))
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

        const string className = $"RazorStatic_{nameof(ITailwindBuilder)}_Impl";

        context.AddSource(
            $"{className}.g.cs",
            $$"""
              using Microsoft.Extensions.Logging;
              using RazorStatic.Shared;
              using System;
              using System.Diagnostics;
              using System.Linq;
              using System.Text.RegularExpressions;
              using System.Threading.Tasks;

              namespace RazorStatic.Shared
              {
                  internal sealed class {{className}} : {{nameof(ITailwindBuilder)}}
                  {
              #nullable enable
                      private static readonly Regex _splitProcessOutputRegex = new Regex("\\s{2,}");
                      
                      private readonly ILogger<{{className}}> _logger;
                      
                      public {{className}}(ILogger<{{className}}> logger)
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
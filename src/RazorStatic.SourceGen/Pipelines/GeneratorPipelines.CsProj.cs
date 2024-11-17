using Microsoft.CodeAnalysis.Diagnostics;
using RazorStatic.SourceGen.Utilities;
using System;
using System.Threading;

namespace RazorStatic.SourceGen.Pipelines;

internal static partial class GeneratorPipelines
{
    public static Func<AnalyzerConfigOptionsProvider, CancellationToken, CsProjProperties> ReadCsProjPipeline() =>
        static (provider, _) => DirectoryUtils.ReadCsProj(provider.GlobalOptions);
}
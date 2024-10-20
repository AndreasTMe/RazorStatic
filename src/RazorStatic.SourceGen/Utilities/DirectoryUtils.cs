﻿using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace RazorStatic.SourceGen.Utilities;

internal static partial class DirectoryUtils
{
    public static CsProjProperties ReadCsProj(AnalyzerConfigOptions options)
    {
        options.TryGetValue("build_property.ProjectDir", out var projectDir);
        options.TryGetValue("build_property.OutputPath", out var outputPath);

        return new CsProjProperties
        {
            ProjectDir = projectDir?.TrimEnd(Path.DirectorySeparatorChar),
            OutputPath = projectDir + Evaluate(outputPath?.TrimEnd(Path.DirectorySeparatorChar)),
        };
    }

    private static string Evaluate(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir))
            return string.Empty;

        if (dir.StartsWith(Path.DirectorySeparatorChar))
            return dir.Length == 1 ? string.Empty : dir[1..];

        return dir;
    }

    public static string GetPageType(string file, string projectDir, string assemblyName)
    {
        var relativePath      = Path.GetRelativePath(projectDir, file);
        var relativeNamespace = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '.');
        var className         = ConvertToClassName(Path.GetFileNameWithoutExtension(file));

        return $"typeof({assemblyName}.{relativeNamespace}.{className})";
    }

    private static string ConvertToClassName(string input)
    {
        var sanitized = InvalidClassNameCharactersRegex().Replace(input, "_");
        return char.IsDigit(sanitized[0])
            ? "_" + sanitized
            : sanitized;
    }

    [GeneratedRegex("[^a-zA-Z0-9_]")]
    private static partial Regex InvalidClassNameCharactersRegex();
}
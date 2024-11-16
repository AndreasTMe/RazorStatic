using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RazorStatic.SourceGen.Utilities;

internal static class DirectoryUtils
{
    public static CsProjProperties ReadCsProj(AnalyzerConfigOptions options)
    {
        options.TryGetValue("build_property.ProjectDir", out var projectDir);
        options.TryGetValue("build_property.OutputPath", out var outputPath);

        projectDir ??= "";
        outputPath ??= "";

        return new CsProjProperties(
            projectDir.TrimEnd(Path.DirectorySeparatorChar),
            projectDir + Evaluate(outputPath.TrimEnd(Path.DirectorySeparatorChar)));
    }

    private static string Evaluate(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir))
            return string.Empty;

        if (dir!.StartsWith(Path.DirectorySeparatorChar.ToString()))
            return dir.Length == 1 ? string.Empty : dir.Substring(1);

        return dir;
    }

    public static string GetPageType(string filePath, string projectDir, string assemblyName)
    {
        var relativePath      = GetRelativePath(projectDir, filePath);
        var relativeNamespace = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '.');
        var className         = ConvertToClassName(Path.GetFileNameWithoutExtension(filePath));

        return $"typeof({assemblyName}.{relativeNamespace}.{className})";
    }

    private static string GetRelativePath(string path1, string path2)
    {
        var split1 = path1.Split(Path.DirectorySeparatorChar);
        var split2 = path2.Split(Path.DirectorySeparatorChar);

        var current = 0;
        while (current < split1.Length && current < split2.Length)
        {
            if (split1[current] != split2[current])
            {
                break;
            }

            current++;
        }

        return string.Join(Path.DirectorySeparatorChar.ToString(), split2.Skip(current));
    }

    private static string ConvertToClassName(string input)
    {
        var sanitized = new Regex("[^a-zA-Z0-9_]").Replace(input, "_");
        return char.IsDigit(sanitized[0])
            ? "_" + sanitized
            : sanitized;
    }
}
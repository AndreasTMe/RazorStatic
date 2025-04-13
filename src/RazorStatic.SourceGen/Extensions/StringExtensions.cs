using System.IO;

namespace RazorStatic.SourceGen.Extensions;

internal static class StringExtensions
{
    public static string EnsurePathSeparator(this string path) =>
        path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
}
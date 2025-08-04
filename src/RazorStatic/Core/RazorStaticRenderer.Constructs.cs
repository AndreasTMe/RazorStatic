using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RazorStatic.Core;

internal sealed partial class RazorStaticRenderer
{
    private sealed class RazorRoute
    {
        public string FullPath  { get; }
        public int    Depth     { get; }
        public bool   IsDynamic { get; }

        public RazorRoute(string fullPath)
        {
            FullPath  = fullPath;
            Depth     = fullPath.Count(static p => p == Path.DirectorySeparatorChar);
            IsDynamic = RegexHelpers.IsDynamicPathRegex().Match(fullPath).Success;
        }
    }

    private static partial class RegexHelpers
    {
        [GeneratedRegex(@"\\\[[a-zA-Z]([a-zA-Z0-9_]?)+\]\.razor$")]
        internal static partial Regex IsDynamicPathRegex();
    }
}
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace RazorStatic.Shared.Utilities;

public static class Constants
{
    public static class Page
    {
        public const string Index = nameof(Index);

        public const string Error404 = "404";
        public const string Error500 = "500";

        private static readonly FrozenSet<string> Reserved = new HashSet<string>
        {
            Index.ToLowerInvariant(),
            Error404,
            Error500
        }.ToFrozenSet();

        internal static bool IsIndex(string pageName) =>
            pageName.Equals(Index, StringComparison.InvariantCultureIgnoreCase);

        internal static bool IsReserved(string pageName) => Reserved.Contains(pageName);
    }

    internal static class Tailwind
    {
        public const string Output = "razor-static-generated-tailwind";
    }

    public static class Static
    {
        public const string CssDirectory    = "_css";
        public const string JsDirectory     = "_js";
        public const string ImagesDirectory = "_images";
    }
}
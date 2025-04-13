using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace RazorStatic.Utilities;

internal static class Constants
{
    public const int BatchSize = 10;

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
}
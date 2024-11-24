using System.Text.RegularExpressions;

namespace RazorStatic.Utilities;

public static partial class SlugUtils
{
    public static string Convert(string input)
    {
        input = SymbolsRegex().Replace(input, string.Empty).Trim();
        input = WhitespaceAndMinusRegex().Replace(input, "-");
        input = SharpRegex().Replace(input, "sharp");
        input = PlusRegex().Replace(input, "plus");

        return input.ToLowerInvariant();
    }

    [GeneratedRegex(@"[^\w\s\-\+#]")]
    private static partial Regex SymbolsRegex();

    [GeneratedRegex(@"[\-\s]+")]
    private static partial Regex WhitespaceAndMinusRegex();

    [GeneratedRegex("#")]
    private static partial Regex SharpRegex();

    [GeneratedRegex(@"\+")]
    private static partial Regex PlusRegex();
}
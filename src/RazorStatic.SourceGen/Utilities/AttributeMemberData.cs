using System.Collections.Immutable;

namespace RazorStatic.SourceGen.Utilities;

internal readonly record struct AttributeMemberData(ImmutableDictionary<string, string> Properties);
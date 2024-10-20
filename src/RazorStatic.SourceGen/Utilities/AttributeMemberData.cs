using System.Collections.Frozen;

namespace RazorStatic.SourceGen.Utilities;

internal readonly record struct AttributeMemberData(FrozenDictionary<string, string> Properties);
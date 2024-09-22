using System.Collections.Frozen;
using System.Collections.Immutable;

namespace RazorStatic.SourceGen.Utilities;

internal readonly record struct AttributeClassInfo(string ClassName,
                                                   string Namespace,
                                                   ImmutableArray<string> Modifiers,
                                                   FrozenDictionary<string, string> Properties);